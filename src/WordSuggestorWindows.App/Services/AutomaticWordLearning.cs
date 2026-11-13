using System.IO;
using System.Security.Cryptography;
using System.Text;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

// UI/platform orchestration only. Core owns token ranges and lexical eligibility.
// One in-flight operation, no durable queue, no raw-text diagnostics or retries.
public sealed class AutomaticWordLearning : IDisposable
{
    private readonly ICoreCompletedWordAdapter _boundary;
    private readonly UnknownWordImportService _import;
    private readonly WindowsAppSettingsService _settings;
    private readonly Func<long> _clock;
    private readonly Dictionary<string,long> _attempts = [];
    private readonly Queue<long> _minute = new();
    private CancellationTokenSource? _pending;
    private Func<bool>? _current;
    private long _last = long.MinValue / 2;
    private bool _disposed;
    public bool IsBusy => _pending is not null;
    public AutomaticWordLearning(ICoreCompletedWordAdapter boundary, ICoreUnknownWordAdapter core,
        WindowsAppSettingsService settings, LearnedWordStore store, Func<long>? clock = null)
    { _boundary=boundary; _import=new(core,settings,store);_settings=settings;_clock=clock??(()=>Environment.TickCount64); }
    public void Revalidate() { if (_current is not null && (!Enabled() || !_current())) Cancel(); }
    public void Cancel() => _pending?.Cancel();
    private bool Enabled() => !_disposed && _settings.Load() is {SelectedLanguageCode:"da-DK", IsUnknownWordLookupEnabled:true, UnknownWordLookupConsentVersion:1, IsAutomaticUnknownWordLookupEnabled:true, UnknownWordAutomaticConsentVersion:1};
    public async Task<string> ObserveAsync(string prefix, CoreRequestContext scope, Func<bool> current)
    {
        Revalidate();
        if (!Enabled() || !current()) return "disabled";
        if (_pending is not null) return "busy";
        var now=_clock();
        while(_minute.TryPeek(out var t) && now-t>=60000)_minute.Dequeue();
        if(now-_last<3000 || _minute.Count>=10)return "rate_limited";
        using var cancellation=new CancellationTokenSource();_pending=cancellation;_current=current;
        bool Current()=>!cancellation.IsCancellationRequested && Enabled() && current();
        try
        {
            // Clipping is a UTF-16 transport limit, never tokenization. Core
            // rejects a token touching the clipped start and returns local ranges.
            var offset=Math.Max(0,prefix.Length-512);
            if(offset>0&&char.IsLowSurrogate(prefix[offset]))offset++;
            var context=prefix[offset..];
            var result=await _boundary.CompletedWordAsync(context,offset,scope,cancellation.Token);
            if(!Current() || result.Context!=scope)return "cancelled";
            if(result.Value.Start is not int start || result.Value.Length is not int length)return "not_candidate";
            var word=context.Substring(start,length);
            var key=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.Language+"\0"+word)));
            now=_clock();if(_attempts.TryGetValue(key,out var prior)&&now-prior<1200000)return "duplicate";
            if(_attempts.Count>=256)_attempts.Clear();_attempts[key]=now;_last=now;_minute.Enqueue(now);
            return await _import.ImportAsync(word,scope,Current,cancellation.Token);
        }
        catch(OperationCanceledException){return "cancelled";}
        catch(Exception ex)when(ex is IOException or InvalidOperationException or ArgumentException or TimeoutException or UnauthorizedAccessException or NotSupportedException or System.Text.Json.JsonException){return "unavailable";}
        finally{_pending=null;_current=null;}
    }
    public void Dispose(){_disposed=true;Cancel();}
}
