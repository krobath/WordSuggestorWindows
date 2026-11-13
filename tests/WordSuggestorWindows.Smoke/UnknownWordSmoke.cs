using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using WordSuggestorWindows.App;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

internal static class UnknownWordSmoke
{
    private const string Mode="WORDSUGGESTOR_TEST_UNKNOWN_MODE";
    private const string Word="zzzultrakandidat";
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    public static bool IsChild(string[] args)=>Environment.GetEnvironmentVariable(Mode) is not null && args.Any(a=>a is "--stdio-json-v1" or "--stdio-json-v2");
    public static int Bridge(string[] args)
    {
        var mode=Environment.GetEnvironmentVariable(Mode);
        var input=Console.In.ReadToEnd(); using var doc=JsonDocument.Parse(input);var q=doc.RootElement;
        if(mode=="real")
        {
            var start=new ProcessStartInfo(Environment.GetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_CLI")!) {UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardInputEncoding=new UTF8Encoding(false)};
            start.ArgumentList.Add(args.Single(a=>a.StartsWith("--stdio-json"))); start.ArgumentList.Add("--lookup-fixture");start.ArgumentList.Add(Environment.GetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_FIXTURE")!);
            using var child=Process.Start(start)!;var output=child.StandardOutput.ReadToEndAsync();var error=child.StandardError.ReadToEndAsync();child.StandardInput.Write(input);child.StandardInput.Close();
            if(!child.WaitForExit(18000)){child.Kill(true);return 3;}Console.Write(output.Result);return child.ExitCode;
        }
        var id=q.GetProperty("requestId").GetString();
        if(q.GetProperty("operation").GetString()=="capabilities")
        {Console.Write(JsonSerializer.Serialize(new {version=1,requestId=id,capabilities=new CoreCapabilities{SchemaVersion=1,ProtocolVersions=[1,2],Operations=["lookupUnknownWord","mergeLearnedWord"],UnknownWordLookup=mode!="unsupported",ExplicitUnknownWordImport=true,LearnedWordOverlay=true}},Json));return 0;}
        if(mode=="hang")Thread.Sleep(30000);
        var body=new Dictionary<string,object?>{{"schemaVersion",1},{"status",mode=="status"?"invented":"not_found"}};
        if(mode=="content")body["text"]=Word;
        var reply=new Dictionary<string,object?>{{"version",2},{"requestId",mode=="identity"?"wrong":id},{"context",q.GetProperty("context").Clone()},{"language",q.GetProperty("language").GetString()},{"unknownWord",body}};
        if(mode=="root")reply["text"]=Word;
        var result=JsonSerializer.Serialize(reply,Json);if(mode=="duplicate")result=result.Replace("\"status\":\"not_found\"","\"status\":\"not_found\",\"status\":\"not_found\"");Console.Write(result);return 0;
    }
    public static async Task Run(string repo,bool desktop)
    {
        var root=Path.Combine(repo,".artifacts","tests","unknown-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT",root);
        var manual=Path.Combine(root,"UserWords","local_lexicon_overlay_v2.json");Directory.CreateDirectory(Path.GetDirectoryName(manual)!);File.WriteAllText(manual,Controlled.Empty);
        var count=0;void Check(string title,bool ok){if(!ok)throw new Exception(title);count++;Console.WriteLine("PASS UNKNOWN WORD "+title);}
        async Task Reject(string title,Func<Task> action){Exception? error=null;try{await action();}catch(Exception e){error=e;}Check(title,error is not null && !error.Message.Contains(Word));}
        var settings=new WindowsAppSettingsService(Path.Combine(root,"settings.json"));settings.Save(new());
        var scope=new CoreRequestContext(Guid.NewGuid(),1);var store=new LearnedWordStore(root);
        var fake=new Controlled();var service=new UnknownWordImportService(fake,settings,store);
        Check("default has no consent",!settings.Load().IsUnknownWordLookupEnabled);
        Check("no consent never contacts Core",await service.ImportAsync(Word,scope,()=>true,default)=="consent_required" && fake.Calls==0);
        void Consent(bool on){var s=settings.Load();s.IsUnknownWordLookupEnabled=on;s.UnknownWordLookupConsentVersion=on?1:0;settings.Save(s);}
        Consent(true);fake.AfterLookup=()=>Consent(false);
        await Reject("revocation after lookup prevents prepare and persistence",()=>service.ImportAsync(Word,scope,()=>true,default));Check("revoked lookup has no store",fake.Merges==0&&!File.Exists(store.StorePath));
        Consent(true);fake.AfterLookup=null;
        var current=true;fake.AfterLookup=()=>current=false;
        await Reject("stale dialog scope prevents prepare",()=>service.ImportAsync(Word,scope,()=>current,default));
        fake.AfterLookup=null;fake.AfterMerge=()=>Consent(false);
        await Reject("revocation after prepare prevents atomic commit",()=>service.ImportAsync(Word,scope,()=>true,default));Check("revoked prepare has no store",!File.Exists(store.StorePath));
        Consent(true);fake.AfterMerge=null;
        Check("explicit service saves prepared bytes",await service.ImportAsync(Word,scope,()=>true,default)=="saved" && File.ReadAllText(store.StorePath)==Controlled.Empty);
        var before=File.ReadAllBytes(store.StorePath);
        fake.AfterMerge=()=>File.WriteAllText(store.StorePath,Controlled.Other);
        await Reject("external preimage change blocks replacement",()=>service.ImportAsync(Word,scope,()=>true,default));Check("changed preimage preserved",File.ReadAllText(store.StorePath)==Controlled.Other);
        File.WriteAllBytes(store.StorePath,before);fake.AfterMerge=null;
        using(var cancellation=new CancellationTokenSource()){cancellation.Cancel();await Reject("cancelled request cannot write",()=>service.ImportAsync(Word,scope,()=>true,cancellation.Token));Check("cancel preserves bytes",File.ReadAllBytes(store.StorePath).SequenceEqual(before));}
        using(var lease=await store.AcquireAsync(default))
        {using var cancelled=new CancellationTokenSource(150);await Reject("cross-instance lease excludes concurrent writers",async()=>{using var second=await new LearnedWordStore(root).AcquireAsync(cancelled.Token);});}
        foreach(var data in new[]{"broken","{\"schema_version\":\"future\",\"entries_by_language\":{}}"})
        {File.WriteAllText(store.StorePath,data);await Reject("invalid existing store protected",()=>service.ImportAsync(Word,scope,()=>true,default));Check("invalid bytes unchanged",File.ReadAllText(store.StorePath)==data);}
        File.WriteAllText(store.StorePath,Controlled.Empty);File.WriteAllText(store.StorePath+".bak","{\"schema_version\":\"future\",\"entries_by_language\":{}}");
        await Reject("import preserves future backup",()=>service.ImportAsync(Word,scope,()=>true,default));
        await Reject("clear preserves future backup",()=>store.ClearAsync(default));Check("future backup and main survive",File.Exists(store.StorePath+".bak")&&File.Exists(store.StorePath));
        File.WriteAllText(store.StorePath+".bak",Controlled.Empty);await store.ClearAsync(default);Check("explicit clear removes only learned main and backup",!File.Exists(store.StorePath)&&!File.Exists(store.StorePath+".bak"));
        var migration=Path.Combine(root,"v6.json");var old="{\"schemaVersion\":6,\"isUnknownWordLookupEnabled\":true,\"unknownWordLookupConsentVersion\":1,\"futureConsent\":false}";File.WriteAllText(migration,old);
        var migrated=new WindowsAppSettingsService(migration);var loaded=migrated.Load();Check("v6 never creates consent",loaded.SchemaVersion==SettingsSchema.CurrentVersion&&!loaded.IsUnknownWordLookupEnabled&&loaded.UnknownWordLookupConsentVersion==0&&!loaded.AdditionalProperties["futureConsent"].GetBoolean());migrated.Save(loaded);Check("migration saves exact original",File.ReadAllText(migration+".v6.bak")==old);
        var actual=new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root);var cli=Environment.GetEnvironmentVariable("WORDSUGGESTOR_SUGGEST_CLI_PATH")??Path.GetFullPath(Path.Combine(repo,"../WordSuggestorCore/.build/x86_64-unknown-windows-msvc/debug/WordSuggestorSuggestCLI.exe"));
        var fixture=Path.Combine(root,"provider.json");File.WriteAllText(fixture,JsonSerializer.Serialize(new[]{new{termNorm=Word,display=Word,gram="sb",lemmaNorm=Word,freq=10,source="fixture",sourceRecordID="fixture:1"}}));
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_CLI",cli);Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_FIXTURE",fixture);Environment.SetEnvironmentVariable(Mode,"real");
        try
        {
            var provider=new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root,cliPath:Environment.ProcessPath);
            var realService=new UnknownWordImportService(provider,settings,store);Consent(true);
            Check("adapter and real Core import validated dictionary data",await realService.ImportAsync(Word,scope,()=>true,default)=="saved");
            Check("real Core learns separate from manual user words",File.ReadAllText(manual)==Controlled.Empty);
            Check("new provider sees learned word after restart",(await new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root).SuggestWithOptionsAsync("zzzultra",scope,new())).Value.Any(r=>r.Term==Word));
            var bytes=File.ReadAllBytes(store.StorePath);Consent(false);Check("opt-out retains local learned suggestions",(await actual.SuggestWithOptionsAsync("zzzultra",scope,new())).Value.Any(r=>r.Term==Word)&&File.ReadAllBytes(store.StorePath).SequenceEqual(bytes));Consent(true);
            Check("repeat import idempotent",await realService.ImportAsync(Word,scope,()=>true,default)=="not_candidate"&&File.ReadAllBytes(store.StorePath).SequenceEqual(bytes));
            await Reject("adapter rejects absent consent",()=>provider.LookupUnknownWordAsync(Word,scope,0));
            await Reject("adapter rejects sentence before transport",()=>provider.LookupUnknownWordAsync("two words",scope,1));
            using var logs=new WindowsDiagnostics(Path.Combine(root,"logs"));logs.Apply(new(){IsCoreDebugLoggingEnabled=true});provider.Diagnostics=logs;
            await provider.LookupUnknownWordAsync("zzzanotherword",scope,1);await logs.FlushAsync();
            var log=string.Concat(Directory.GetFiles(Path.Combine(root,"logs")).Select(File.ReadAllText));Check("diagnostics contain operation only without words or paths",log.Contains("LookupUnknownWord")&&!log.Contains("zzzanotherword")&&!log.Contains(root));provider.Diagnostics=null;
            foreach(var mode in new[]{"unsupported","identity","status","content","root","duplicate"})
            {Environment.SetEnvironmentVariable(Mode,mode);var bad=new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root,cliPath:Environment.ProcessPath);await Reject("invalid capability or reply rejected: "+mode,()=>bad.LookupUnknownWordAsync(Word,scope,1));}
            Environment.SetEnvironmentVariable(Mode,"hang");using(var cts=new CancellationTokenSource(250)){var hanging=new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root,cliPath:Environment.ProcessPath);await Reject("cancellation terminates dictionary process",()=>hanging.LookupUnknownWordAsync(Word,scope,1,cts.Token));}
            Environment.SetEnvironmentVariable(Mode,"real");await store.ClearAsync(default);
            if(desktop)await Desktop(settings,provider,store,Check);
        }
        finally{Environment.SetEnvironmentVariable(Mode,null);Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_CLI",null);Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_FIXTURE",null);}
        Console.WriteLine($"PASS UNKNOWN WORD {count} checks; desktop={desktop}; explicit only; no sharing or passive capture.");
    }
    private static async Task Desktop(WindowsAppSettingsService settings,WordSuggestorCoreCliSuggestionProvider provider,LearnedWordStore store,Action<string,bool> check)
    {
        var start=settings.Load();start.IsUnknownWordLookupEnabled=false;start.UnknownWordLookupConsentVersion=0;settings.Save(start);
        using var vm=new MainWindowViewModel(provider,new WindowsErrorInsightsStore(),settings);
        var allow=false;var form=new SettingsWindow(vm,confirmUnknownLookup:()=>allow);UnknownWordLookupWindow? window=null;
        try
        {
            form.Show();form.Activate();((ListBox)form.FindName("CategoryNavigation")).SelectedIndex=7;await Task.Delay(250);
            var toggle=(CheckBox)form.FindName("UnknownWordLookupCheckBox");
            Toggle(toggle);check("declined consent stays off",!settings.Load().IsUnknownWordLookupEnabled&&toggle.IsChecked==false);
            allow=true;Toggle(toggle);check("approved consent persists version one",settings.Load().IsUnknownWordLookupEnabled&&settings.Load().UnknownWordLookupConsentVersion==1);
            window=new UnknownWordLookupWindow(vm,Word,provider,store,()=>true){Owner=form};window.Show();window.Activate();
            var activationDeadline=Environment.TickCount64+5000;while(!window.IsActive&&Environment.TickCount64<activationDeadline)await Task.Delay(25);
            check("dialog is active before explicit action",window.IsActive);
            var controls=Descendants(window).OfType<FrameworkElement>().Where(e=>!string.IsNullOrEmpty(AutomationProperties.GetAutomationId(e))).ToDictionary(AutomationProperties.GetAutomationId);
            check("dialog has accessible input actions and live status",new[]{"UnknownWordInput","UnknownWordLookup","UnknownWordConsent","UnknownWordCancel","UnknownWordClear","UnknownWordStatus"}.All(id=>controls.ContainsKey(id)&&!string.IsNullOrWhiteSpace(AutomationProperties.GetName(controls[id]))));
            var lookup=(Button)controls["UnknownWordLookup"];((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(lookup)!.GetPattern(PatternInterface.Invoke)!).Invoke();
            var deadline=Environment.TickCount64+10000;while((!File.Exists(store.StorePath)||((TextBlock)controls["UnknownWordStatus"]).Text!=SettingsText.Get("UnknownSaved"))&&Environment.TickCount64<deadline)await Task.Delay(25);
            if(!File.Exists(store.StorePath)||((TextBlock)controls["UnknownWordStatus"]).Text!=SettingsText.Get("UnknownSaved")) Console.WriteLine("UNKNOWN UI FINAL STATE active="+window.IsActive+" status="+((TextBlock)controls["UnknownWordStatus"]).Text);
            check("native dialog imports through actual Core and atomic store",File.Exists(store.StorePath)&&((TextBlock)controls["UnknownWordStatus"]).Text==SettingsText.Get("UnknownSaved"));
            Toggle((CheckBox)controls["UnknownWordConsent"]);check("dialog revokes shared setting",!settings.Load().IsUnknownWordLookupEnabled);
            SettingsText.ApplyPreference("en-GB");check("localized import strings present",SettingsText.Get("UnknownSaved").StartsWith("The word"));SettingsText.ApplyPreference("da-DK");
            window.Close();await store.ClearAsync(default);
            var enabled=settings.Load();enabled.IsUnknownWordLookupEnabled=true;enabled.UnknownWordLookupConsentVersion=1;settings.Save(enabled);
            Environment.SetEnvironmentVariable(Mode,"hang");
            window=new UnknownWordLookupWindow(vm,"zzzanotherword",provider,store,()=>true){Owner=form};window.Show();window.Activate();
            var closingControls=Descendants(window).OfType<FrameworkElement>().Where(e=>!string.IsNullOrEmpty(AutomationProperties.GetAutomationId(e))).ToDictionary(AutomationProperties.GetAutomationId);
            var closeAction=(Button)closingControls["UnknownWordLookup"];
            ((IInvokeProvider)UIElementAutomationPeer.CreatePeerForElement(closeAction)!.GetPattern(PatternInterface.Invoke)!).Invoke();
            await Task.Delay(200);check("pending dialog exposes cancellation",((Button)closingControls["UnknownWordCancel"]).IsEnabled);
            window.Close();await Task.Delay(400);check("closing pending dialog prevents late local store",!File.Exists(store.StorePath));
            Environment.SetEnvironmentVariable(Mode,"real");
        }
        finally{window?.Close();form.Close();SettingsText.ApplyPreference("da-DK");}
    }
    private static void Toggle(CheckBox c)=>((IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(c)!.GetPattern(PatternInterface.Toggle)!).Toggle();
    private static IEnumerable<DependencyObject> Descendants(DependencyObject p){for(var i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(p);i++){var c=System.Windows.Media.VisualTreeHelper.GetChild(p,i);yield return c;foreach(var d in Descendants(c))yield return d;}}
    private sealed class Controlled:ICoreUnknownWordAdapter
    {
        public const string Empty="{\"schema_version\":\"local_lexicon_overlay_v2\",\"entries_by_language\":{}}";
        public const string Other="{ \"schema_version\":\"local_lexicon_overlay_v2\",\"entries_by_language\":{} }";
        public int Calls,Merges;public Action? AfterLookup,AfterMerge;
        public Task<CoreQueryResult<CoreUnknownWordResult>> LookupUnknownWordAsync(string word,CoreRequestContext scope,int consentVersion,CancellationToken token=default)
        {Calls++;AfterLookup?.Invoke();return Task.FromResult(new CoreQueryResult<CoreUnknownWordResult>(scope,"da-DK",new(1,"resolved",JsonSerializer.SerializeToElement(new{display=word}),null),null));}
        public Task<CoreQueryResult<CoreUnknownWordResult>> PrepareLearnedWordAsync(string word,JsonElement proposal,CoreRequestContext scope,int consentVersion,CancellationToken token=default)
        {Merges++;AfterMerge?.Invoke();return Task.FromResult(new CoreQueryResult<CoreUnknownWordResult>(scope,"da-DK",new(1,"prepared",null,Empty),null));}
    }
}
