using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

namespace WordSuggestorWindows.App.ViewModels;

public sealed record AnalysisDocumentSnapshot(CoreRequestContext Context, string Text, string Language, string PackSha256, CoreDocumentResult Result)
{
    public override string ToString() => $"AnalysisDocument generation={Context.Generation}, length={Text.Length}";
}

public sealed partial class MainWindowViewModel
{
    private readonly WindowsWordChoiceStore _wordChoices = new();
    private CancellationTokenSource? _analysisCancellation;
    private long _analysisGeneration;
    private readonly Guid _analysisDocumentId = Guid.NewGuid();
    private sealed record AnalysisPackIdentity(string Stamp, string Hash);
    private AnalysisPackIdentity? _analysisPackIdentity;
    private AppSettingsSnapshot? _lastAnalysisSettings;
    public AnalysisDocumentSnapshot? AnalysisDocument { get; private set; }
    private readonly Dictionary<int, string> _readingOverrides = [];
    public string DisplayWordClass(CoreDocumentWord word) => _readingOverrides.GetValueOrDefault(word.Start, word.WordClass);
    public string AnalysisStatus { get; private set; } = "";
    public event EventHandler? SavedChoicesChanged;
    public Func<AnalysisEditTransaction, bool>? AnalysisCommitRequested { get; set; }

    private void InitializeDocumentAnalysis()
    {
        _lastAnalysisSettings = LoadSettingsSnapshot();
        PropertyChanged += AnalysisPropertyChanged; SettingsChanged += AnalysisSettingsChanged;
        ScheduleDocumentAnalysis();
    }
    private void DisposeDocumentAnalysis()
    {
        PropertyChanged -= AnalysisPropertyChanged; SettingsChanged -= AnalysisSettingsChanged;
        _analysisGeneration++; _analysisCancellation?.Cancel(); _analysisCancellation?.Dispose(); _analysisCancellation = null;
    }
    private void AnalysisPropertyChanged(object? sender, PropertyChangedEventArgs e)
    { if (e.PropertyName is nameof(EditorText) or nameof(SelectedLanguageOption)) ScheduleDocumentAnalysis(); }
    private void AnalysisSettingsChanged(object? sender, AppSettingsSnapshot settings)
    {
        var old = _lastAnalysisSettings; _lastAnalysisSettings = settings.Clone();
        if (old is null || old.IsTextAnalyzerEnabled != settings.IsTextAnalyzerEnabled || old.IsTextAnalyzerColoringEnabled != settings.IsTextAnalyzerColoringEnabled ||
            old.IsSemanticDiagnosticsEnabled != settings.IsSemanticDiagnosticsEnabled || old.IsPunctuationDiagnosticsEnabled != settings.IsPunctuationDiagnosticsEnabled ||
            old.PunctuationCommaStyle != settings.PunctuationCommaStyle || old.ContextualCommaMode != settings.ContextualCommaMode)
            ScheduleDocumentAnalysis();
    }
    public void ScheduleDocumentAnalysis()
    {
        var generation = ++_analysisGeneration;
        _analysisCancellation?.Cancel(); _analysisCancellation?.Dispose();
        var cancellation = new CancellationTokenSource(); _analysisCancellation = cancellation; var token = cancellation.Token;
        AnalysisDocument = null;
        _readingOverrides.Clear();
        var text = EditorText; var language = SelectedLanguageOption.LanguageCode; var pack = SelectedLanguageOption.PackPath;
        var settings = LoadSettingsSnapshot();
        AnalysisStatus = !settings.IsTextAnalyzerEnabled ? SettingsText.Get("AnalysisOff") : text.Length > 16000 ? SettingsText.Get("AnalysisTooLarge") : "";
        NotifyAnalysisState();
        if (!settings.IsTextAnalyzerEnabled || text.Length == 0 || text.Length > 16000 || CoreSettings is not { } adapter) return;
        var scope = new CoreRequestContext(_analysisDocumentId, generation);
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, token);
                var capabilities = await adapter.GetCapabilitiesAsync(cancellationToken: token);
                if (!capabilities.DocumentReadings) throw new NotSupportedException();
                var result = await adapter.AnalyzeDocumentAsync(text, scope, new(
                    Semantic: capabilities.ContextDiagnostics && settings.IsSemanticDiagnosticsEnabled,
                    Punctuation: capabilities.ContextDiagnostics && settings.IsPunctuationDiagnosticsEnabled,
                    CommaStyle: settings.PunctuationCommaStyle, CommaMode: settings.ContextualCommaMode), token);
                var hash = "";
                if (pack is not null && File.Exists(pack))
                {
                    var stamp = pack + ":" + new FileInfo(pack).Length + ":" + File.GetLastWriteTimeUtc(pack).Ticks;
                    if (_analysisPackIdentity is { } cached && cached.Stamp == stamp) hash = cached.Hash;
                    else { using var stream = File.OpenRead(pack); hash = Convert.ToHexString(SHA256.HashData(stream)); _analysisPackIdentity = new(stamp, hash); }
                }
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (token.IsCancellationRequested || generation != _analysisGeneration || EditorText != text || SelectedLanguageOption.LanguageCode != language || !result.IsCurrent(scope, language)) return;
                    AnalysisDocument = new(scope, text, language, hash, result.Value);
                    AnalysisStatus = SettingsText.Format(result.Value.Readings.Truncated ? "AnalysisPartial" : "AnalysisReady", result.Value.Readings.Words.Length, result.Value.Diagnostics.Count);
                    NotifyAnalysisState();
                });
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException or IOException or UnauthorizedAccessException or ArgumentException or TimeoutException)
            {
                await App.Current.Dispatcher.InvokeAsync(() => { if (token.IsCancellationRequested || generation != _analysisGeneration) return; AnalysisStatus = SettingsText.Get("AnalysisUnavailable"); NotifyAnalysisState(); });
            }
        }, token);
    }
    private void NotifyAnalysisState()
    {
        OnPropertyChanged(nameof(AnalysisDocument)); OnPropertyChanged(nameof(AnalysisStatus));
        OnPropertyChanged(nameof(EditorReadinessSummary));
        OnPropertyChanged(nameof(AnalyzerLegendMetrics)); OnPropertyChanged(nameof(SpellingCount)); OnPropertyChanged(nameof(GrammarCount)); OnPropertyChanged(nameof(StatusMetrics));
    }
    private bool CurrentAnalysis(AnalysisDocumentSnapshot snapshot) => ReferenceEquals(snapshot, AnalysisDocument) && snapshot.Context.Generation == _analysisGeneration && snapshot.Text == EditorText && snapshot.Language == SelectedLanguageOption.LanguageCode && _appliedSettings.IsTextAnalyzerEnabled;
    public bool TryApplyDiagnostic(CoreDiagnostic diagnostic, int alternative = 0)
    {
        if (AnalysisDocument is not { } snapshot || !CurrentAnalysis(snapshot) || !snapshot.Result.Diagnostics.Contains(diagnostic) || CanCommitLocally?.Invoke() != true) return false;
        AnalysisEditTransaction transaction;
        try { transaction = DiagnosticEditPlanner.Create(EditorText, CaretIndex, diagnostic, alternative); }
        catch (InvalidOperationException) { return false; }
        if (AnalysisCommitRequested is { } commit)
        { if (!commit(transaction)) return false; }
        else
        {
            _caretIndex = _selectionStart = _selectionEnd = transaction.Caret; _editorText = transaction.Result;
            OnPropertyChanged(nameof(EditorText)); OnPropertyChanged(nameof(CaretIndex)); NotifyEditorMetricsChanged();
        }
        if (_isErrorTrackingEnabled)
        {
            string? example = null;
            if (_appliedSettings.StoreSentenceExamples && diagnostic.ExampleStart is { } start && diagnostic.ExampleLength is { } length)
                example = snapshot.Text.Substring(start, length);
            _insightsStore.RecordDiagnosticCorrection(snapshot.Language, diagnostic.IssueType, example);
        }
        return true;
    }
    public bool TryChooseReading(CoreDocumentWord word, CoreSurfaceAnalysis reading)
    {
        if (AnalysisDocument is not { } snapshot || !CurrentAnalysis(snapshot) || !word.Ambiguous ||
            !snapshot.Result.Readings.Words.Contains(word) || !word.Readings.Contains(reading) || snapshot.PackSha256.Length != 64 || word.Length > 128) return false;
        if (_appliedSettings.SaveWordChoices && !ChangeChoices(() => _wordChoices.Append(new(Guid.NewGuid(), DateTimeOffset.UtcNow, snapshot.Language, snapshot.Text.Substring(word.Start, word.Length), reading.Id, reading.WordClass, reading.Gram, snapshot.PackSha256)))) return false;
        _readingOverrides[word.Start] = reading.WordClass; NotifyAnalysisState(); return true;
    }
    public IReadOnlyList<SavedWordChoice> LoadSavedChoices() => _wordChoices.Load();
    public bool DeleteSavedChoice(Guid id) => ChangeChoices(() => _wordChoices.Delete(id));
    public bool DeleteAllSavedChoices() => ChangeChoices(_wordChoices.Reset);
    public bool ResetLocalLearning()
    {
        var before = LoadSettingsSnapshot(); var next = before.Clone(); next.SaveWordChoices = next.ShareWordChoices = false;
        return TryApplySettingsChanges(before, next) && DeleteAllSavedChoices();
    }
    private bool ChangeChoices(Action change)
    {
        try { change(); SavedChoicesChanged?.Invoke(this, EventArgs.Empty); return true; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.Text.Json.JsonException)
        { StatusMessage = SettingsText.Get("LearningUnavailable"); return false; }
    }
    private int CountWordClass(string wordClass) => AnalysisDocument?.Result.Readings.Words.Count(w => DisplayWordClass(w) == wordClass) ?? 0;
    private int CountDiagnostic(string kind) => AnalysisDocument?.Result.Diagnostics.Count(d => d.IssueType == kind) ?? 0;
}
