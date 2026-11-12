using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

// Bounded, process-local, device-local metadata. Callers cannot submit arbitrary
// strings. Producers never wait for disk; only explicit test/maintenance flushes wait.
public sealed class WindowsDiagnostics : IDisposable
{
    public const int MaximumFileBytes = 64 * 1024;
    public const int RetainedFiles = 3;
    private sealed record Options(bool App, bool Placement, bool Panel, bool Core, bool Performance);
    private sealed record Pending(long Epoch, RuntimeDiagnostic? Record = null, TaskCompletionSource? Barrier = null);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly Channel<Pending> _queue = Channel.CreateBounded<Pending>(new BoundedChannelOptions(128) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true });
    private readonly string _directory;
    private readonly Task _writer;
    private readonly Timer _summaryTimer;
    private readonly object _metricGate = new();
    private readonly Dictionary<DiagnosticMetric, (long Count, long Total, long Max)> _metrics = [];
    private readonly object _rateGate = new();
    private long _rateSecond;
    private int _rateCount;
    private long _epoch, _dropped;
    private int _health, _disposed;
    private Options _options = new(false, false, false, false, false);

    public WindowsDiagnostics(string? directory = null)
    {
        _directory = Path.GetFullPath(directory ?? Path.Combine(WindowsDataPaths.Root, "diagnostics", "runtime"));
        _writer = Task.Run(WriteLoop);
        _summaryTimer = new Timer(_ => FlushPerformance(), null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
    }
    public DiagnosticHealth Health => (DiagnosticHealth)Volatile.Read(ref _health);
    public event EventHandler? HealthChanged;
    private void SetHealth(DiagnosticHealth health)
    { if (Interlocked.Exchange(ref _health, (int)health) != (int)health) HealthChanged?.Invoke(this, EventArgs.Empty); }
    private void SetWriterHealth(DiagnosticHealth health)
    {
        // An in-flight append must not overwrite a later disable/dispose status.
        while (true)
        {
            var current = Volatile.Read(ref _health);
            if (current == (int)DiagnosticHealth.Disabled) return;
            if (Interlocked.CompareExchange(ref _health, (int)health, current) != current) continue;
            if (current != (int)health) HealthChanged?.Invoke(this, EventArgs.Empty);
            return;
        }
    }
    public long DroppedCount => Interlocked.Read(ref _dropped);
    public bool Enabled(DiagnosticCategory category)
    {
        if (Volatile.Read(ref _disposed) != 0) return false;
        var o = Volatile.Read(ref _options);
        return category switch { DiagnosticCategory.App => o.App, DiagnosticCategory.Placement => o.Placement, DiagnosticCategory.Panel => o.Panel, DiagnosticCategory.Core => o.Core, DiagnosticCategory.Performance => o.Performance, _ => false };
    }
    public void Apply(AppSettingsSnapshot settings)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        Volatile.Write(ref _options, new(settings.IsAppDebugLoggingEnabled, settings.IsPlacementDebugLoggingEnabled, settings.IsPanelParityLoggingEnabled, settings.IsCoreDebugLoggingEnabled, settings.IsPerformanceInstrumentationEnabled));
        Interlocked.Increment(ref _epoch);
        lock (_metricGate) _metrics.Clear();
        SetHealth(settings.IsAppDebugLoggingEnabled || settings.IsPlacementDebugLoggingEnabled || settings.IsPanelParityLoggingEnabled || settings.IsCoreDebugLoggingEnabled || settings.IsPerformanceInstrumentationEnabled ? DiagnosticHealth.Ready : DiagnosticHealth.Disabled);
    }
    public void Record(DiagnosticCategory category, DiagnosticEvent kind, DiagnosticOperation operation = DiagnosticOperation.None)
    {
        if (!Enabled(category) || !Enum.IsDefined(kind) || !Enum.IsDefined(operation)) return;
        Enqueue(new(1, DateTimeOffset.UtcNow, category, kind, operation));
    }
    public void RecordCore(CoreOperationalDiagnostics summary)
    {
        var operation = Operation(summary.Operation);
        if (operation == DiagnosticOperation.None || !summary.ValidFor(summary.Operation, summary.Status == "rejected") || !Enabled(DiagnosticCategory.Core)) return;
        Enqueue(new(1, DateTimeOffset.UtcNow, DiagnosticCategory.Core, DiagnosticEvent.CoreSummary, operation,
            TotalMicroseconds: summary.ElapsedMilliseconds * 1000, CandidateCount: summary.CandidateCount, DiagnosticCount: summary.DiagnosticCount, DocumentWordCount: summary.DocumentWordCount));
    }
    public static DiagnosticOperation Operation(string operation) => operation switch
    { "suggest" => DiagnosticOperation.Suggest, "capabilities" => DiagnosticOperation.Capabilities, "domains" => DiagnosticOperation.Domains, "domainSuggestions" => DiagnosticOperation.DomainSuggestions, "wordInfo" => DiagnosticOperation.WordInfo, "analyze" => DiagnosticOperation.Analyze, "document" => DiagnosticOperation.Document, "smartFollowup" => DiagnosticOperation.SmartFollowup, "livePeriod" => DiagnosticOperation.LivePeriod, "assessCommittedWord" => DiagnosticOperation.AssessCommittedWord, "lookupUnknownWord" => DiagnosticOperation.LookupUnknownWord, "mergeLearnedWord" => DiagnosticOperation.MergeLearnedWord, _ => DiagnosticOperation.None };
    public Measurement Measure(DiagnosticMetric metric) => new(this, metric, Enabled(DiagnosticCategory.Performance));
    public readonly struct Measurement : IDisposable
    {
        private readonly WindowsDiagnostics _owner; private readonly DiagnosticMetric _metric; private readonly long _start, _epoch;
        internal Measurement(WindowsDiagnostics owner, DiagnosticMetric metric, bool enabled) { _owner = owner; _metric = metric; _epoch = Interlocked.Read(ref owner._epoch); _start = enabled ? Stopwatch.GetTimestamp() : 0; }
        public void Dispose() { if (_start != 0 && _epoch == Interlocked.Read(ref _owner._epoch)) _owner.RecordTiming(_metric, Stopwatch.GetElapsedTime(_start)); }
    }
    public void RecordTiming(DiagnosticMetric metric, TimeSpan elapsed)
    {
        if (!Enabled(DiagnosticCategory.Performance) || metric == DiagnosticMetric.None || !Enum.IsDefined(metric) || elapsed < TimeSpan.Zero) return;
        var micros = (long)Math.Clamp(elapsed.TotalMicroseconds, 0, 60_000_000);
        lock (_metricGate)
        {
            var old = _metrics.GetValueOrDefault(metric);
            if (old.Count >= 1_000_000) return;
            _metrics[metric] = (old.Count + 1, old.Total + micros, Math.Max(old.Max, micros));
        }
    }
    public void FlushPerformance()
    {
        lock (_metricGate)
        {
            if (Enabled(DiagnosticCategory.Performance))
                foreach (var (metric, value) in _metrics)
                    Enqueue(new(1, DateTimeOffset.UtcNow, DiagnosticCategory.Performance, DiagnosticEvent.Aggregate, Metric: metric, Count: value.Count, TotalMicroseconds: value.Total, MaxMicroseconds: value.Max));
            _metrics.Clear();
        }
    }
    private void Enqueue(RuntimeDiagnostic record)
    {
        lock (_rateGate)
        {
            var second = Environment.TickCount64 / 1000;
            if (_rateSecond != second) { _rateSecond = second; _rateCount = 0; }
            if (++_rateCount > 128) { Interlocked.Increment(ref _dropped); return; }
        }
        if (!_queue.Writer.TryWrite(new(Interlocked.Read(ref _epoch), record))) Interlocked.Increment(ref _dropped);
    }
    public async Task FlushAsync(CancellationToken cancellation = default)
    {
        FlushPerformance();
        var barrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await _queue.Writer.WriteAsync(new(Interlocked.Read(ref _epoch), Barrier: barrier), cancellation);
        await barrier.Task.WaitAsync(cancellation);
    }
    private async Task WriteLoop()
    {
        await foreach (var pending in _queue.Reader.ReadAllAsync())
        {
            if (pending.Barrier is { } barrier) { barrier.TrySetResult(); continue; }
            if (pending.Record is not { } row || pending.Epoch != Interlocked.Read(ref _epoch) || !Enabled(row.Category)) continue;
            try
            {
                Directory.CreateDirectory(_directory);
                var path = Path.Combine(_directory, "runtime-v1.jsonl");
                var line = JsonSerializer.Serialize(row, Json) + "\n";
                for (var i = 0; i < RetainedFiles; i++)
                {
                    var old = i == 0 ? path : path + "." + i;
                    if (File.Exists(old) && File.GetLastWriteTimeUtc(old) < DateTime.UtcNow.AddDays(-7)) File.Delete(old);
                }
                if (File.Exists(path) && new FileInfo(path).Length + Encoding.UTF8.GetByteCount(line) > MaximumFileBytes)
                {
                    File.Delete(path + ".2");
                    if (File.Exists(path + ".1")) File.Move(path + ".1", path + ".2");
                    File.Move(path, path + ".1");
                }
                // A disabled/reconfigured category discards queued records. An
                // already-started metadata-only append may finish after a toggle.
                if (pending.Epoch != Interlocked.Read(ref _epoch) || !Enabled(row.Category)) continue;
                await File.AppendAllTextAsync(path, line, new UTF8Encoding(false));
                SetWriterHealth(DiagnosticHealth.Ready);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            { SetWriterHealth(DiagnosticHealth.WriteUnavailable); }
        }
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Interlocked.Increment(ref _epoch); _summaryTimer.Dispose(); _queue.Writer.TryComplete();
        lock (_metricGate) _metrics.Clear();
        SetHealth(DiagnosticHealth.Disabled);
    }
    public Task Completion => _writer;
}
