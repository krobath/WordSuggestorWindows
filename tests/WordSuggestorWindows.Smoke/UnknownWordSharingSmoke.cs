using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

internal static class UnknownWordSharingSmoke
{
    public static async Task Run(string repo)
    {
        var root=Path.Combine(repo,".artifacts","tests","sharing-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT",root);
        var count=0;void Check(string label,bool ok){if(!ok)throw new Exception(label);count++;Console.WriteLine("PASS UNKNOWN SHARING "+label);}
        var scope=new CoreRequestContext(Guid.NewGuid(),4);var settings=new WindowsAppSettingsService();settings.Save(new(){IsUnknownWordLookupEnabled=true,UnknownWordLookupConsentVersion=1});
        var manual=Path.Combine(root,"UserWords","local_lexicon_overlay_v2.json");Directory.CreateDirectory(Path.GetDirectoryName(manual)!);File.WriteAllText(manual,"{\"schema_version\":\"local_lexicon_overlay_v2\",\"entries_by_language\":{}}");
        var cli=Environment.GetEnvironmentVariable("WORDSUGGESTOR_SUGGEST_CLI_PATH")??Path.GetFullPath(Path.Combine(repo,"../WordSuggestorCore/.build/x86_64-unknown-windows-msvc/debug/WordSuggestorSuggestCLI.exe"));
        var fixture=Path.Combine(root,"dictionary.json");File.WriteAllText(fixture,"[{\"termNorm\":\"zzzultrakandidat\",\"display\":\"zzzultrakandidat\",\"gram\":\"sb\",\"freq\":10,\"source\":\"fixture\",\"sourceRecordID\":\"fixture:1\"}]");
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_MODE","real");Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_CLI",cli);Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_FIXTURE",fixture);
        try
        {
            var provider=new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root,cliPath:Environment.ProcessPath);
            Check("real Core empty overlay prepares no payload",(await provider.PrepareUnknownWordHandoffAsync(scope,1,"test")).Value.Status=="empty");
            var store=new LearnedWordStore(root);Check("real Core import seeds learned fixture",await new UnknownWordImportService(provider,settings,store).ImportAsync("zzzultrakandidat",scope,()=>true,default)=="saved");
            var prepared=await provider.PrepareUnknownWordHandoffAsync(scope,1,"test");var parsed=UnknownWordHandoffPacket.Parse(prepared.Value.Body!);
            Check("wire format and idempotency come from existing shared Core",parsed.Body.GetProperty("schema_version").GetString()=="unknown_word_crowd_handoff_ingest_v1"&&parsed.Key.StartsWith("uwh_")&&parsed.Body.GetProperty("handoff").GetProperty("entries").GetArrayLength()==1);
            Check("DTO never prints lexical payload",!prepared.Value.ToString().Contains("zzzultrakandidat"));
            foreach(var damaged in new[]{"{}",prepared.Value.Body!.Replace("\"handoff\":", "\"document_text\":\"PRIVATE\",\"handoff\":"),prepared.Value.Body!.Replace("\"display\":", "\"raw_text\":\"PRIVATE\",\"display\":")})
            {var refused=false;try{_=UnknownWordHandoffPacket.Parse(damaged);}catch(InvalidOperationException e){refused=!e.Message.Contains("PRIVATE");}Check("malformed or content-bearing packet fails closed",refused);}
            var core=new FakeCore(prepared.Value);var sender=new Sender();var permission=true;var configured=false;var now=DateTimeOffset.UtcNow;
            using var client=new UnknownWordSharingClient(core,sender,()=>permission,()=>configured,root,()=>now);
            Check("unconfigured deployment performs no work",await client.SynchronizeAsync(scope,"test")=="backend_unavailable"&&core.Calls==0&&sender.Calls==0&&!File.Exists(client.StorePath));
            configured=true;permission=false;Check("no independent consent never prepares or sends",await client.SynchronizeAsync(scope,"test")=="consent_or_login_required"&&core.Calls==0);
            permission=true;sender.Ready=false;Check("signed-out session cannot queue",await client.SynchronizeAsync(scope,"test")=="consent_or_login_required");sender.Ready=true;
            sender.Failure=new("offline");Check("offline keeps one durable packet",await client.SynchronizeAsync(scope,"test")=="retry_pending"&&File.Exists(client.StorePath));var key=sender.Keys.Single();var bytes=File.ReadAllBytes(client.StorePath);
            Check("backoff prevents immediate retry",await client.SynchronizeAsync(scope,"test")=="backoff"&&sender.Calls==1&&File.ReadAllBytes(client.StorePath).SequenceEqual(bytes));
            now=now.AddSeconds(11);sender.Failure=null;
            using(var reopened=new UnknownWordSharingClient(core,sender,()=>permission,()=>configured,root,()=>now))Check("restart sends the identical persisted idempotency key",await reopened.SynchronizeAsync(scope,"new-version")=="delivered"&&sender.Keys.Last()==key&&core.Calls==1);
            Check("acknowledgement removes lexical body and queue backup",!File.ReadAllText(client.StorePath).Contains("zzzultrakandidat")&&!File.Exists(client.StorePath+".bak"));
            Check("unchanged store never resends",await client.SynchronizeAsync(scope,"test")=="unchanged"&&sender.Calls==2);
            sender.UserId=2;Check("account switch cannot reassign queued or delivered data",await client.SynchronizeAsync(scope,"test")=="account_mismatch"&&sender.Calls==2);sender.UserId=1;
            Check("explicit revoke clears queue only",await client.RevokeAsync()=="cleared"&&!File.Exists(client.StorePath)&&File.Exists(store.StorePath)&&File.ReadAllText(manual).Contains("local_lexicon_overlay_v2"));
            sender.Failure=new("offline");await client.SynchronizeAsync(scope,"test");var sendsBeforeChange=sender.Calls;var learnedBytes=File.ReadAllBytes(store.StorePath);
            File.AppendAllText(store.StorePath,"\n");core.Value=new(1,"empty",null);
            Check("changed or cleared source cannot send old queued words",await client.SynchronizeAsync(scope,"test")=="empty"&&sender.Calls==sendsBeforeChange&&!File.ReadAllText(client.StorePath).Contains("zzzultrakandidat")&&!File.Exists(client.StorePath+".bak"));
            File.WriteAllBytes(store.StorePath,learnedBytes);core.Value=prepared.Value;await client.RevokeAsync();
            sender.Failure=new("rate_limited",429);Check("429 queues bounded retry",await client.SynchronizeAsync(scope,"test")=="retry_pending");
            sender.Failure=new("unauthorized",401);now=now.AddSeconds(11);Check("401 stops automatic retries",await client.SynchronizeAsync(scope,"test")=="login_or_permission_required"&&await client.SynchronizeAsync(scope,"test")=="retry_exhausted");await client.RevokeAsync();
            sender.Failure=new("offline");await client.SynchronizeAsync(scope,"test");now=now.AddDays(8);Check("pending data expires after seven days",await client.SynchronizeAsync(scope,"test")=="expired");Check("expiry removes the queued lexical body and backup",!File.ReadAllText(client.StorePath).Contains("zzzultrakandidat")&&!File.Exists(client.StorePath+".bak"));await client.RevokeAsync();
            sender.Failure=null;sender.BeforeSend=()=>permission=false;
            Check("revocation during send cannot record success",await client.SynchronizeAsync(scope,"test")=="cancelled"&&File.ReadAllText(client.StorePath).Contains("pending"));permission=true;sender.BeforeSend=null;await client.RevokeAsync();
            foreach(var bad in new[]{"{bad","{\"schemaVersion\":99}"}){File.WriteAllText(client.StorePath,bad);var calls=sender.Calls;Check("corrupt/future queue is preserved without sending",await client.SynchronizeAsync(scope,"test")=="unavailable"&&File.ReadAllText(client.StorePath)==bad&&sender.Calls==calls);File.Delete(client.StorePath);}
            File.WriteAllText(client.StorePath+".bak","{\"schemaVersion\":99}");Check("future backup cannot be overwritten or cleared",await client.SynchronizeAsync(scope,"test")=="unavailable"&&await client.RevokeAsync()=="unavailable"&&File.Exists(client.StorePath+".bak"));File.Delete(client.StorePath+".bak");
            var initial=settings.Load();initial.SchemaVersion=8;initial.IsUnknownWordCrowdHandoffEnabled=true;initial.UnknownWordSharingConsentVersion=1;initial.UnknownWordSharingConsentUserId=1;
            Check("migration never manufactures sharing consent",SettingsSchema.Upgrade(initial) is {IsUnknownWordCrowdHandoffEnabled:false,UnknownWordSharingConsentVersion:0,UnknownWordSharingConsentUserId:0});
            Check("production remains source-gated with no guessed endpoint",!UnknownWordSharingDeployment.Qualified&&UnknownWordSharingDeployment.EndpointPath is null);
            var capture=new CaptureHandler();using var transport=new AccountBackendClient("https://fixture.invalid",capture);
            await transport.SendAsync<bool>(HttpMethod.Post,"/v1/fixture",parsed.Body,"synthetic-token",default,parsed.Key);
            Check("existing HTTPS transport preserves payload and idempotency",capture.Key==parsed.Key&&capture.Body==parsed.Body.GetRawText()&&capture.HasBearer);
            capture.Status=HttpStatusCode.Redirect;var rejected=false;try{await transport.SendAsync<bool>(HttpMethod.Post,"/v1/fixture",parsed.Body,"synthetic-token",default,parsed.Key);}catch(AccountFailure e){rejected=e.Status==302&&!e.Message.Contains("synthetic-token");}
            Check("redirect is rejected without credential forwarding",rejected&&capture.Calls==2);
        }
        finally{foreach(var key in new[]{"WORDSUGGESTOR_TEST_UNKNOWN_MODE","WORDSUGGESTOR_TEST_UNKNOWN_CLI","WORDSUGGESTOR_TEST_UNKNOWN_FIXTURE"})Environment.SetEnvironmentVariable(key,null);}
        Console.WriteLine($"PASS UNKNOWN SHARING {count} checks; existing Core wire format; no public backend upload.");
    }
    sealed class FakeCore(CoreUnknownWordHandoff value):ICoreUnknownWordHandoffAdapter
    {public int Calls;public CoreUnknownWordHandoff Value=value;public Task<CoreQueryResult<CoreUnknownWordHandoff>> PrepareUnknownWordHandoffAsync(CoreRequestContext scope,int consent,string version,CancellationToken token=default){Calls++;return Task.FromResult(new CoreQueryResult<CoreUnknownWordHandoff>(scope,"da-DK",Value,null));}}
    sealed class Sender:IUnknownWordSharingSender
    {
        public string Origin=>"https://fixture.invalid";public long UserId{get;set;}=1;public bool Ready{get;set;}=true;public int Calls;public List<string> Keys=[];public AccountFailure? Failure;public Action? BeforeSend;
        public Task SendAsync(JsonElement body,string key,CancellationToken token){Calls++;Keys.Add(key);BeforeSend?.Invoke();if(Failure is not null)throw Failure;token.ThrowIfCancellationRequested();return Task.CompletedTask;}
    }
    sealed class CaptureHandler:HttpMessageHandler
    {
        public string? Key,Body;public bool HasBearer;public int Calls;public HttpStatusCode Status=HttpStatusCode.Accepted;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){Calls++;Key=request.Headers.GetValues("Idempotency-Key").Single();Body=await request.Content!.ReadAsStringAsync(token);HasBearer=request.Headers.Authorization?.Scheme=="Bearer";return new(Status){Content=new StringContent("PRIVATE_RESPONSE_MUST_NOT_BE_LOGGED")};}
    }
}
