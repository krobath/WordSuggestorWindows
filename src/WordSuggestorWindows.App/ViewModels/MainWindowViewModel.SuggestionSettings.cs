using System.IO;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

namespace WordSuggestorWindows.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private IReadOnlyList<SuggestionItem> _primarySuggestions = [], _secondarySuggestions = [];
    private int _primaryPage, _secondaryPage;
    public bool ShowPrimaryLane { get; private set; }
    public bool ShowSecondaryLane { get; private set; }
    public bool HasPrimarySuggestions => _primarySuggestions.Count > 0;
    public bool HasSecondarySuggestions => _secondarySuggestions.Count > 0;
    public string PrimaryLaneSummary => $"{SettingsText.Get("F18.Title")} · {_primaryPage + 1}/{Math.Max(1, (_primarySuggestions.Count + 9) / 10)}";
    public string SecondaryLaneSummary => $"{SettingsText.Get("F19.Title")} · {_secondaryPage + 1}/{Math.Max(1, (_secondarySuggestions.Count + 9) / 10)}";
    public string SuggestionShortcutSummary => $"{SuggestionShortcutPolicy.Label(_appliedSettings.Suggestions.OrdinaryShortcut, 0)}…{SuggestionShortcutPolicy.Label(_appliedSettings.Suggestions.OrdinaryShortcut, 9)} · Ctrl+←/→";
    public double SuggestionOverlayWidth => 292 * (1 + (ShowPrimaryLane ? 1 : 0) + (ShowSecondaryLane ? 1 : 0));
    public IReadOnlyList<SuggestionOverlayEntry> VisiblePrimarySuggestions => DomainEntries(_primarySuggestions, _primaryPage, _appliedSettings.Suggestions.PrimaryShortcut);
    public IReadOnlyList<SuggestionOverlayEntry> VisibleSecondarySuggestions => DomainEntries(_secondarySuggestions, _secondaryPage, _appliedSettings.Suggestions.SecondaryShortcut);
    private IReadOnlyList<SuggestionOverlayEntry> DomainEntries(IReadOnlyList<SuggestionItem> rows, int page, string shortcut) => rows.Skip(page * 10).Take(10)
        .Select((item, i) => new SuggestionOverlayEntry(i, SuggestionShortcutPolicy.Label(shortcut, i), item, item == SelectedSuggestion) { Density = _appliedSettings.Suggestions.Density }).ToArray();
    public bool AcceptLaneAtIndex(int lane, int index)
    {
        var rows = lane switch { 0 => VisibleSuggestions, 1 => VisiblePrimarySuggestions, 2 => VisibleSecondarySuggestions, _ => [] };
        if (index < 0 || index >= rows.Count) return false;
        SelectedSuggestion = rows[index].Suggestion;
        return TryAcceptSelectedSuggestion();
    }
    public void ChangeLanePage(int lane, int delta)
    {
        if (lane == 0) { ChangeSuggestionPage(delta); return; }
        var count = lane == 1 ? _primarySuggestions.Count : _secondarySuggestions.Count;
        var pages = Math.Max(1, (count + 9) / 10);
        if (lane == 1) _primaryPage = ((_primaryPage + delta) % pages + pages) % pages;
        else _secondaryPage = ((_secondaryPage + delta) % pages + pages) % pages;
        NotifyDomainSuggestions();
    }
    private bool ContainsCurrentSuggestion(SuggestionItem item) => Suggestions.Contains(item) || _primarySuggestions.Contains(item) || _secondarySuggestions.Contains(item);
    private void ClearDomainSuggestions(bool keepVisible) => SetDomainSuggestions([], [], keepVisible);
    private void SetDomainSuggestions(IReadOnlyList<SuggestionItem> primary, IReadOnlyList<SuggestionItem> secondary, bool retainVisibility = false)
    {
        if (!retainVisibility) { ShowPrimaryLane = primary.Count > 0; ShowSecondaryLane = secondary.Count > 0; }
        _primarySuggestions = primary; _secondarySuggestions = secondary; _primaryPage = _secondaryPage = 0; NotifyDomainSuggestions();
    }
    private void NotifyDomainSuggestions()
    {
        OnPropertyChanged(nameof(VisiblePrimarySuggestions)); OnPropertyChanged(nameof(VisibleSecondarySuggestions));
        OnPropertyChanged(nameof(HasPrimarySuggestions)); OnPropertyChanged(nameof(HasSecondarySuggestions)); OnPropertyChanged(nameof(SuggestionOverlayWidth));
        OnPropertyChanged(nameof(PrimaryLaneSummary)); OnPropertyChanged(nameof(SecondaryLaneSummary)); OnPropertyChanged(nameof(HasSuggestions));
        OnPropertyChanged(nameof(ShowPrimaryLane)); OnPropertyChanged(nameof(ShowSecondaryLane)); OnPropertyChanged(nameof(SuggestionShortcutSummary));
    }
    private Guid _suggestionDocumentId = Guid.NewGuid();
    public event EventHandler<SuggestionItem>? LocalSuggestionCommitted;
    private readonly SuggestionSchedulingPolicy _suggestionScheduler = new();
    public ICoreSettingsAdapter? CoreSettings => _suggestionProvider as ICoreSettingsAdapter;
    public string ExternalSuggestionSuffix { get; set; } = "";
    public ExternalTextSnapshot? ExternalSuggestionTarget { get; set; }
    private ExternalTextSnapshot? _currentCommitTarget;
    public string CurrentSuggestionSuffix
    {
        get
        {
            var suffix = _isLocalEditorActive ? EditorCommit.Suffix(EditorText, _selectionStart, _selectionEnd) : ExternalSuggestionSuffix;
            return EditorCommit.PrefixWindow(suffix, 64);
        }
    }
    public bool IsSuggestionInteractionEnabled { get; private set; } = true;
    public void SetSuggestionInteractionEnabled(bool enabled)
    { if (IsSuggestionInteractionEnabled == enabled) return; IsSuggestionInteractionEnabled = enabled; OnPropertyChanged(nameof(IsSuggestionInteractionEnabled)); }
    public void HideIdleSuggestions() => ClearSuggestionSession(keepOverlayVisible: false);
    public bool AcceptFamilyForm(SuggestionItem original, CoreFamilyForm family)
    {
        if (!_appliedSettings.Suggestions.WordForms || !ContainsCurrentSuggestion(original) || family.CommitPlan is null) return false;
        var item = original with { Term = family.Term, InsertText = family.Term, CommitPlan = family.CommitPlan };
        // A family click enters the same verified transaction as every candidate.
        Suggestions.Add(item);
        try { return AcceptSuggestion(item); }
        finally { Suggestions.Remove(item); }
    }
    public CoreRequestContext SuggestionScope => new(_suggestionDocumentId, _requestRevision);
    public string? ResolveDomainCatalog()
    {
        // Only installed catalogs are admitted. Core's development seed is never
        // searched, copied or silently promoted to a product language pack.
        var path = Path.Combine(WindowsDataPaths.Root, "Packs", SelectedLanguageOption.LanguageCode, "specialized", "catalog.json");
        return File.Exists(path) ? path : null;
    }
    private async Task<IReadOnlyList<SuggestionItem>> QuerySuggestionsAsync(string text, CoreRequestContext scope,
        string suffix, SuggestionPreferences preferences, CancellationToken cancellationToken)
    {
            // The production adapter already owns its bounded process gate.
            // An uncooperative legacy provider must not block a newer request;
            // generation checks still discard its eventual stale completion.
            if (CoreSettings is not { } adapter) return await _suggestionProvider.SuggestAsync(text, cancellationToken).WaitAsync(cancellationToken);
            var language = SelectedLanguageOption.LanguageCode;
            var caps = await adapter.GetCapabilitiesAsync(cancellationToken: cancellationToken);
            if (!caps.SourcePreferences) return await _suggestionProvider.SuggestAsync(text, cancellationToken).WaitAsync(cancellationToken);
            var result = await adapter.SuggestWithOptionsAsync(text, scope, new(
                ConfusionSet: preferences.ConfusionSet,
                Phonetic: caps.Phonetic ? preferences.Phonetic : null,
                Misspelling: caps.Misspellings ? preferences.Misspelling : null,
                Synonym: caps.Synonyms ? preferences.Synonym : null,
                ContextBased: caps.ContextReranking ? preferences.ContextBased : null,
                SpacingMode: caps.CommitPlans ? preferences.Spacing : null,
                Suffix: caps.CommitPlans ? EditorCommit.PrefixWindow(suffix, 64) : null), cancellationToken);
            if (!result.IsCurrent(scope, language)) throw new OperationCanceledException(cancellationToken);
            return result.Value.Select(c => new SuggestionItem(c.Term, c.Score, c.Kind, c.Type, c.Pos, c.Gram)
            { InsertText = c.InsertText, CommitPlan = c.CommitPlan }).ToArray();
    }
    private sealed record SuggestionLanes(IReadOnlyList<SuggestionItem> Ordinary, IReadOnlyList<SuggestionItem> Primary, IReadOnlyList<SuggestionItem> Secondary);
    private async Task<SuggestionLanes> QuerySuggestionLanesAsync(string text, CoreRequestContext scope, string suffix, SuggestionPreferences preferences, CancellationToken cancellationToken)
    {
        text = EditorCommit.TailWindow(text, 16000);
        var language = SelectedLanguageOption.LanguageCode;
        var catalog = ResolveDomainCatalog();
        var enabled = _appliedSettings.IsDomainListsEnabled;
        var ordinary = await QuerySuggestionsAsync(text, scope, suffix, preferences, cancellationToken);
        IReadOnlyList<SuggestionItem> primary = [], secondary = [];
        if (CoreSettings is { } adapter && enabled && catalog is not null)
        {
            var caps = await adapter.GetCapabilitiesAsync(catalog, cancellationToken);
            if (caps.DomainCatalog)
            {
                var catalogResult = await adapter.GetDomainsAsync(catalog, scope, cancellationToken);
                if (!catalogResult.IsCurrent(scope, language)) throw new OperationCanceledException(cancellationToken);
                async Task<IReadOnlyList<SuggestionItem>> Query(string? id, int lane)
                {
                    if (id is null || !catalogResult.Value.Any(d => d.Id == id)) return [];
                    var response = await adapter.SuggestDomainAsync(text, catalog, id, scope, cancellationToken,
                        new(SpacingMode: caps.CommitPlans ? preferences.Spacing : null, Suffix: caps.CommitPlans ? EditorCommit.PrefixWindow(suffix, 64) : null));
                    if (!response.IsCurrent(scope, language)) throw new OperationCanceledException(cancellationToken);
                    return response.Value.Select(c => new SuggestionItem(c.Term, c.Score, c.Kind, c.Type, c.Pos, c.Gram) { InsertText = c.InsertText, CommitPlan = c.CommitPlan, Lane = lane }).ToArray();
                }
                var primaryId = preferences.PrimaryDomains.GetValueOrDefault(language);
                primary = await Query(primaryId, 1);
                if (primaryId is not null && catalogResult.Value.Any(d => d.Id == primaryId) && preferences.SecondaryDomains.GetValueOrDefault(language) != primaryId)
                    secondary = await Query(preferences.SecondaryDomains.GetValueOrDefault(language), 2);
            }
        }
        return new(ordinary, primary, secondary);
    }
}
