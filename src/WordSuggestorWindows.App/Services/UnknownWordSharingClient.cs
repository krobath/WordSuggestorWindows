using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

// Deployment is deliberately source-reviewed. No user JSON/environment variable
// can enable an unprovisioned receiver or choose another credential destination.
public static class UnknownWordSharingDeployment
{
    public static bool Qualified => false;
    public static string? EndpointPath => null;
    public const string Capability="unknown_word_crowd_handoff_v1_ingest";
}
public interface IUnknownWordSharingSender
{
    string Origin {get;}
    long UserId {get;}
    bool Ready {get;}
    Task SendAsync(JsonElement body,string idempotencyKey,CancellationToken token);
}
public sealed class UnknownWordSharingClient : IDisposable
{
    private readonly ICoreUnknownWordHandoffAdapter _core;
    private readonly IUnknownWordSharingSender _sender;
    private readonly Func<bool> _permission;
    private readonly Func<bool> _configured;
    private readonly LearnedWordStore _learned;
    private readonly Func<DateTimeOffset> _clock;
    private readonly SemaphoreSlim _serial=new(1,1);
    private CancellationTokenSource? _active;
    private bool _disposed;
    public string StorePath {get;}
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed record State(int SchemaVersion,long UserId,string Origin,string SourceFingerprint,string? Body,DateTimeOffset Created,int Attempts,DateTimeOffset RetryAfter,string Status);
    private const int MaximumStoreBytes=3_145_728;
    private static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web);
    public UnknownWordSharingClient(ICoreUnknownWordHandoffAdapter core,IUnknownWordSharingSender sender,Func<bool> permission,Func<bool> configured,string root,Func<DateTimeOffset>? clock=null)
    {
        var origin=new Uri(sender.Origin);
        if(origin.Scheme!="https"||origin.UserInfo.Length!=0||origin.AbsolutePath!="/"||origin.Query.Length!=0||origin.Fragment.Length!=0)throw new ArgumentException("Invalid sharing origin.");
        _core=core;_sender=sender;_permission=permission;_configured=configured;_learned=new(root);_clock=clock??(()=>DateTimeOffset.UtcNow);
        StorePath=Path.Combine(root,"UnknownWords","sharing-v1.json");
    }
    private bool Allowed(long user)=>!_disposed&&_configured()&&_permission()&&_sender.Ready&&user>0&&_sender.UserId==user;
    public void Cancel()=>_active?.Cancel();
    public async Task<string> SynchronizeAsync(CoreRequestContext scope,string version,CancellationToken token=default)
    {
        if(!_configured())return "backend_unavailable";
        var user=_sender.UserId;if(!Allowed(user))return "consent_or_login_required";
        if(!await _serial.WaitAsync(0,token))return "busy";
        using var active=CancellationTokenSource.CreateLinkedTokenSource(token);_active=active;var cancellation=active.Token;
        try
        {
            using var lease=await Lease(cancellation);var state=Read();
            if(state is not null&&(state.UserId!=user||state.Origin!=_sender.Origin))return "account_mismatch";
            var fingerprint=_learned.Fingerprint();
            if(state?.Body is not null&&state.SourceFingerprint!=fingerprint)
            {
                state=state with{Body=null,SourceFingerprint="",Status="superseded"};
                Write(state,()=>Allowed(user),cancellation);if(File.Exists(StorePath+".bak"))File.Delete(StorePath+".bak");
            }
            if(state?.Body is null)
            {
                if(state?.SourceFingerprint==fingerprint)return "unchanged";
                var prepared=await _core.PrepareUnknownWordHandoffAsync(scope,1,version,cancellation);
                if(!Allowed(user)||cancellation.IsCancellationRequested)return "cancelled";
                if(prepared.Context!=scope)throw new InvalidOperationException("Sharing scope rejected.");
                if(prepared.Value.Status=="empty")
                {
                    if(_learned.Fingerprint()!=fingerprint)return "source_changed";
                    Write(new(1,user,_sender.Origin,fingerprint,null,_clock(),0,_clock(),"expired"),()=>Allowed(user),cancellation);
                    if(File.Exists(StorePath+".bak"))File.Delete(StorePath+".bak");return "empty";
                }
                if(prepared.Value.Status!="prepared")return prepared.Value.Status;
                _=UnknownWordHandoffPacket.Parse(prepared.Value.Body!);
                if(_learned.Fingerprint()!=fingerprint)return "source_changed";
                state=new(1,user,_sender.Origin,fingerprint,prepared.Value.Body,_clock(),0,_clock(),"pending");
                Write(state,()=>Allowed(user),cancellation);
            }
            if(_clock()-state.Created>TimeSpan.FromDays(7))
            {
                Write(state with{Body=null,Status="expired"},()=>Allowed(user),cancellation);
                if(File.Exists(StorePath+".bak"))File.Delete(StorePath+".bak");
                return "expired";
            }
            if(state.Attempts>=5)return "retry_exhausted";
            if(_clock()<state.RetryAfter)return "backoff";
            if(!Allowed(user))return "cancelled";
            if(_learned.Fingerprint()!=state.SourceFingerprint)return "source_changed";
            var packet=UnknownWordHandoffPacket.Parse(state.Body!);
            try
            {
                await _sender.SendAsync(packet.Body,packet.Key,cancellation);
                if(!Allowed(user)||cancellation.IsCancellationRequested)return "cancelled";
                Write(state with{Body=null,Status="delivered"},()=>Allowed(user),cancellation);
                // Do not retain accepted lexical data in an old queue backup.
                if(File.Exists(StorePath+".bak"))File.Delete(StorePath+".bak");
                return "delivered";
            }
            catch(AccountFailure failure)
            {
                if(!Allowed(user))return "cancelled";
                var retry=failure.Code=="offline"||failure.Status is 408 or 429 or >=500;
                var attempts=retry?state.Attempts+1:5;
                var delay=Math.Min(1800,10*Math.Pow(2,attempts-1));
                Write(state with{Attempts=attempts,RetryAfter=_clock().AddSeconds(delay),Status=retry?"retry_pending":"rejected"},()=>Allowed(user),cancellation);
                return retry?"retry_pending":failure.Status is 401 or 403?"login_or_permission_required":"rejected";
            }
        }
        catch(OperationCanceledException){return "cancelled";}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or InvalidOperationException or JsonException or ArgumentException){return "unavailable";}
        finally{_active=null;_serial.Release();}
    }
    public async Task<string> RevokeAsync(CancellationToken token=default)
    {
        Cancel();await _serial.WaitAsync(token);
        try{using var lease=await Lease(token);_=Read();CheckBackup();token.ThrowIfCancellationRequested();if(File.Exists(StorePath+".bak"))File.Delete(StorePath+".bak");if(File.Exists(StorePath))File.Delete(StorePath);return "cleared";}
        catch(Exception e)when(e is IOException or UnauthorizedAccessException or InvalidOperationException or JsonException){return "unavailable";}
        finally{_serial.Release();}
    }
    private async Task<FileStream> Lease(CancellationToken token)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);var end=Environment.TickCount64+2500;
        while(true){token.ThrowIfCancellationRequested();try{return new(StorePath+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);}catch(IOException)when(Environment.TickCount64<end){await Task.Delay(50,token);}}
    }
    private State? Read(string? path=null)
    {
        path??=StorePath;if(!File.Exists(path))return null;if(new FileInfo(path).Length>MaximumStoreBytes)throw new InvalidOperationException("Sharing store unavailable.");
        var bytes=File.ReadAllBytes(path);using var doc=JsonDocument.Parse(bytes);var names=doc.RootElement.EnumerateObject().Select(p=>p.Name).ToArray();
        if(names.Length!=9||names.Distinct(StringComparer.Ordinal).Count()!=9)throw new InvalidOperationException("Sharing store unavailable.");
        var state=JsonSerializer.Deserialize<State>(bytes,Json);
        if(state is not{SchemaVersion:1}||state.UserId<=0||state.Attempts is <0 or >5||state.SourceFingerprint is null||state.Origin is null||state.Status is not("pending" or "retry_pending" or "rejected" or "delivered" or "expired" or "superseded"))throw new InvalidOperationException("Sharing store unavailable.");
        if(state.Body is not null)_=UnknownWordHandoffPacket.Parse(state.Body);else if(state.Status is not("delivered" or "expired" or "superseded"))throw new InvalidOperationException("Sharing store unavailable.");
        return state;
    }
    private void CheckBackup(){if(File.Exists(StorePath+".bak"))_=Read(StorePath+".bak");}
    private void Write(State state,Func<bool> current,CancellationToken token)
    {
        _=Read();CheckBackup();var temp=StorePath+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{var bytes=JsonSerializer.SerializeToUtf8Bytes(state,Json);if(bytes.Length>MaximumStoreBytes)throw new InvalidOperationException("Sharing store unavailable.");using(var file=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(bytes);file.Flush(true);}token.ThrowIfCancellationRequested();if(!current())throw new OperationCanceledException();if(File.Exists(StorePath))File.Replace(temp,StorePath,StorePath+".bak");else File.Move(temp,StorePath);}
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public void Dispose(){_disposed=true;Cancel();}
}
