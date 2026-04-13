using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

namespace WordSuggestorWindows.App.ViewModels;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private const int SuggestionPageSize = 10;
    private const int MaxSuggestionPages = 4;
    private readonly ISuggestionProvider _suggestionProvider;
    private readonly WindowsErrorInsightsStore _insightsStore;
    private readonly WindowsAppSettingsService _settingsService;
    private readonly RelayCommand _acceptSelectedSuggestionCommand;
    private CancellationTokenSource? _suggestionCts;
    private string _editorText = string.Empty;
    private SuggestionItem? _selectedSuggestion;
    private string _statusMessage;
    private bool _isBusy;
    private int _caretIndex;
    private bool _isEditorExpanded;
    private bool _isSuggestionOverlaySessionVisible;
    private bool _isGlobalCaptureEnabled = true;
    private LanguageOption _selectedLanguageOption;
    private bool _isAnalyzerColoringEnabled = true;
    private bool _isSemanticDiagnosticsEnabled;
    private bool _isPunctuationDiagnosticsEnabled;
    private bool _isErrorTrackingEnabled = true;
    private bool _isSpeechToTextListening;
    private bool _isTextToSpeechSpeaking;
    private int _currentSuggestionPage;
    private SuggestionPlacementMode _suggestionPlacementMode = SuggestionPlacementMode.FollowCaret;

    public MainWindowViewModel(
        ISuggestionProvider suggestionProvider,
        WindowsErrorInsightsStore insightsStore,
        WindowsAppSettingsService settingsService,
        string? initialEditorText = null)
    {
        _suggestionProvider = suggestionProvider;
        _insightsStore = insightsStore;
        _settingsService = settingsService;
        var settings = _settingsService.Load();
        _editorText = initialEditorText ?? string.Empty;
        _caretIndex = _editorText.Length;
        _statusMessage = string.IsNullOrWhiteSpace(initialEditorText)
            ? "Windows toolbar shell klar. Udvid editoren for at skrive og hente forslag."
            : "Startuptekst er klar. Åbn editoren for at vise og redigere teksten.";
        LanguageOptions = suggestionProvider.LanguageOptions;
        _selectedLanguageOption = LanguageOptions.FirstOrDefault(option =>
                string.Equals(option.LanguageCode, settings.SelectedLanguageCode, StringComparison.OrdinalIgnoreCase))
            ?? suggestionProvider.SelectedLanguage;
        _suggestionProvider.SetLanguage(_selectedLanguageOption);
        _isGlobalCaptureEnabled = settings.IsGlobalCaptureEnabled;
        _isAnalyzerColoringEnabled = settings.IsTextAnalyzerColoringEnabled;
        _isSemanticDiagnosticsEnabled = settings.IsSemanticDiagnosticsEnabled;
        _isPunctuationDiagnosticsEnabled = settings.IsPunctuationDiagnosticsEnabled;
        _isErrorTrackingEnabled = settings.IsErrorTrackingEnabled;
        _suggestionPlacementMode = ResolveSuggestionPlacementMode(settings.SuggestionPlacementMode);
        Suggestions = [];
        Suggestions.CollectionChanged += SuggestionsOnCollectionChanged;
        _acceptSelectedSuggestionCommand = new RelayCommand(ExecuteAcceptSelectedSuggestion, CanAcceptSelectedSuggestion);

        if (!string.IsNullOrWhiteSpace(_editorText))
        {
            ScheduleSuggestionsRefresh();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SuggestionItem> Suggestions { get; }

    public ICommand AcceptSelectedSuggestionCommand => _acceptSelectedSuggestionCommand;

    public IReadOnlyList<LanguageOption> LanguageOptions { get; }

    public string ProviderDescription => _suggestionProvider.ProviderDescription;

    public IReadOnlyList<EditorStatusMetric> StatusMetrics =>
    [
        new("Aa", "Tegn", CharacterCount.ToString()),
        new("≡", "Ord", WordCount.ToString()),
        new("!", "Stavefejl", SpellingCount.ToString()),
        new("●", "Grammatik/tegnsætning", GrammarCount.ToString())
    ];

    public IReadOnlyList<AnalyzerLegendMetric> AnalyzerLegendMetrics =>
    [
        new("Substantiver", 0, "#C26AF7"),
        new("Egennavne", 0, "#F45A85"),
        new("Verber", 0, "#2CBCCB"),
        new("Tillægsord", 0, "#F5A14E"),
        new("Biord", 0, "#758BFF"),
        new("Pronomen", 0, "#5BC878"),
        new("Determiner", 0, "#A97B58"),
        new("Præpositioner", 0, "#4BB6E8"),
        new("Konjunktioner", 0, "#48C7B3"),
        new("Staveforslag", 0, "#E24A4A", true),
        new("Semantik", 0, "#4A7DF0", true),
        new("Tegnsætning", 0, "#4A7DF0", true)
    ];

    public string EditorText
    {
        get => _editorText;
        set
        {
            if (SetProperty(ref _editorText, value))
            {
                NotifyEditorMetricsChanged();
                if (ShouldClearSuggestionsForBoundaryText(_editorText))
                {
                    ClearSuggestionSession(keepOverlayVisible: _editorText.Length > 0);
                }
                else
                {
                    ScheduleSuggestionsRefresh();
                }
            }
        }
    }

    public SuggestionItem? SelectedSuggestion
    {
        get => _selectedSuggestion;
        set
        {
            if (SetProperty(ref _selectedSuggestion, value))
            {
                _acceptSelectedSuggestionCommand.NotifyCanExecuteChanged();
                OnPropertyChanged(nameof(VisibleSuggestions));
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public int CaretIndex
    {
        get => _caretIndex;
        set => SetProperty(ref _caretIndex, value);
    }

    public bool IsEditorExpanded
    {
        get => _isEditorExpanded;
        private set
        {
            if (SetProperty(ref _isEditorExpanded, value))
            {
                OnPropertyChanged(nameof(ExpandCollapseGlyph));
                OnPropertyChanged(nameof(ExpandCollapseToolTip));
                OnPropertyChanged(nameof(ShouldShowSuggestionOverlay));
            }
        }
    }

    public bool IsGlobalCaptureEnabled
    {
        get => _isGlobalCaptureEnabled;
        set
        {
            if (SetProperty(ref _isGlobalCaptureEnabled, value))
            {
                PersistCurrentSettings();
                StatusMessage = value
                    ? "Global forslag er slået til. Cross-app integration kommer i WSA-RT-003."
                    : "Global forslag er slået fra i Windows-shell'en.";
            }
        }
    }

    public LanguageOption SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set
        {
            if (value is null)
            {
                return;
            }

            if (SetProperty(ref _selectedLanguageOption, value))
            {
                _suggestionProvider.SetLanguage(value);
                PersistCurrentSettings();
                OnPropertyChanged(nameof(ProviderDescription));
                OnPropertyChanged(nameof(OverlaySupportSummary));

                if (!value.IsPackAvailable)
                {
                    ClearSuggestionSession(keepOverlayVisible: IsEditorExpanded && EditorText.Length > 0);
                    StatusMessage = $"{value.DisplayName} er valgt, men sprogpakken mangler.";
                    return;
                }

                StatusMessage = $"{value.DisplayName} er aktivt sprog. {value.PackAvailabilityLabel}.";
                if (ShouldClearSuggestionsForBoundaryText(EditorText))
                {
                    ClearSuggestionSession(keepOverlayVisible: IsEditorExpanded && EditorText.Length > 0);
                }
                else
                {
                    ScheduleSuggestionsRefresh();
                }
            }
        }
    }

    public bool IsAnalyzerColoringEnabled
    {
        get => _isAnalyzerColoringEnabled;
        private set => SetProperty(ref _isAnalyzerColoringEnabled, value);
    }

    public bool IsSemanticDiagnosticsEnabled
    {
        get => _isSemanticDiagnosticsEnabled;
        private set => SetProperty(ref _isSemanticDiagnosticsEnabled, value);
    }

    public bool IsPunctuationDiagnosticsEnabled
    {
        get => _isPunctuationDiagnosticsEnabled;
        private set => SetProperty(ref _isPunctuationDiagnosticsEnabled, value);
    }

    public bool IsSpeechToTextListening
    {
        get => _isSpeechToTextListening;
        private set
        {
            if (SetProperty(ref _isSpeechToTextListening, value))
            {
                OnPropertyChanged(nameof(SpeechToTextToolTip));
                OnPropertyChanged(nameof(SpeechToTextButtonBackground));
            }
        }
    }

    public string SpeechToTextToolTip => IsSpeechToTextListening
        ? "Stop tale til tekst"
        : "Tale til tekst";

    public string SpeechToTextButtonBackground => IsSpeechToTextListening
        ? "#E5EEF8"
        : "Transparent";

    public bool IsTextToSpeechSpeaking
    {
        get => _isTextToSpeechSpeaking;
        private set
        {
            if (SetProperty(ref _isTextToSpeechSpeaking, value))
            {
                OnPropertyChanged(nameof(TextToSpeechToolTip));
                OnPropertyChanged(nameof(TextToSpeechButtonBackground));
            }
        }
    }

    public string TextToSpeechToolTip => IsTextToSpeechSpeaking
        ? "Stop oplæsning"
        : "Oplæs markeret tekst";

    public string TextToSpeechButtonBackground => IsTextToSpeechSpeaking
        ? "#E5EEF8"
        : "Transparent";

    public string ExpandCollapseGlyph => IsEditorExpanded ? "\uE70E" : "\uE70D";

    public string ExpandCollapseToolTip => IsEditorExpanded ? "Skjul editor" : "Vis editor";

    public int CharacterCount => EditorText.Length;

    public int WordCount => EditorText
        .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
        .Length;

    public int SpellingCount => 0;

    public int GrammarCount => 0;

    public SuggestionPlacementMode SuggestionPlacementMode
    {
        get => _suggestionPlacementMode;
        private set
        {
            if (SetProperty(ref _suggestionPlacementMode, value))
            {
                OnPropertyChanged(nameof(IsStaticPlacementMode));
                OnPropertyChanged(nameof(IsFollowCaretPlacementMode));
                OnPropertyChanged(nameof(SuggestionPlacementSummary));
            }
        }
    }

    public bool IsStaticPlacementMode => SuggestionPlacementMode == SuggestionPlacementMode.Static;

    public bool IsFollowCaretPlacementMode => SuggestionPlacementMode == SuggestionPlacementMode.FollowCaret;

    public string SuggestionPlacementSummary => IsFollowCaretPlacementMode
        ? "Follow-caret aktiv"
        : "Statisk placering aktiv";

    public string EditorReadinessSummary => IsAnalyzerColoringEnabled
        ? "Farvekodning og analysepanel er synlige i Windows-baseline."
        : "Farvekodning er slået fra i Windows-baseline.";

    public string AnalyzerToggleSummary =>
        $"Farver: {(IsAnalyzerColoringEnabled ? "til" : "fra")} · Semantik: {(IsSemanticDiagnosticsEnabled ? "til" : "fra")} · Tegnsætning: {(IsPunctuationDiagnosticsEnabled ? "til" : "fra")}";

    public string OverlaySupportSummary =>
        $"{SuggestionPlacementSummary} · {SuggestionPageSummary} · {ProviderDescription}";

    public int CurrentSuggestionPage => _currentSuggestionPage;

    public int TotalSuggestionCount => Suggestions.Count;

    public int TotalSuggestionPages
    {
        get
        {
            if (Suggestions.Count == 0)
            {
                return 1;
            }

            var rawPages = (int)Math.Ceiling((double)Suggestions.Count / SuggestionPageSize);
            return Math.Min(MaxSuggestionPages, Math.Max(1, rawPages));
        }
    }

    public string SuggestionPageSummary => $"Side {CurrentSuggestionPage + 1}/{TotalSuggestionPages}";

    public string SuggestionPanelCountSummary => $"{TotalSuggestionCount} forslag";

    public IReadOnlyList<SuggestionOverlayEntry> VisibleSuggestions
    {
        get
        {
            if (Suggestions.Count == 0)
            {
                return [];
            }

            var start = CurrentSuggestionPage * SuggestionPageSize;
            return Suggestions
                .Skip(start)
                .Take(SuggestionPageSize)
                .Select((suggestion, index) => new SuggestionOverlayEntry(
                    index,
                    index == 9 ? "Ctrl+0" : $"Ctrl+{index + 1}",
                    suggestion,
                    SelectedSuggestion == suggestion))
                .ToArray();
        }
    }

    public bool HasSuggestions => Suggestions.Count > 0;

    public bool ShouldShowSuggestionOverlay => IsEditorExpanded && _isSuggestionOverlaySessionVisible;

    public bool AcceptSelectedSuggestion()
    {
        if (SelectedSuggestion is null)
        {
            return false;
        }

        ExecuteAcceptSelectedSuggestion();
        return true;
    }

    public bool AcceptSuggestion(SuggestionItem suggestion)
    {
        SelectedSuggestion = suggestion;
        return AcceptSelectedSuggestion();
    }

    public bool AcceptSuggestionAtIndex(int visibleIndex)
    {
        var visible = VisibleSuggestions;
        if (visibleIndex < 0 || visibleIndex >= visible.Count)
        {
            return false;
        }

        SelectedSuggestion = visible[visibleIndex].Suggestion;
        return AcceptSelectedSuggestion();
    }

    public void ToggleEditorExpanded()
    {
        IsEditorExpanded = !IsEditorExpanded;
        StatusMessage = IsEditorExpanded
            ? "Editor åbnet i Windows toolbar shell."
            : "Editor skjult. Toolbar shell er tilbage i kompakt tilstand.";
    }

    public void EnsureEditorExpanded()
    {
        if (!IsEditorExpanded)
        {
            IsEditorExpanded = true;
        }
    }

    public void ImportTextIntoEditor(string text, string source)
    {
        var normalized = NormalizeImportedText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            StatusMessage = "Ingen markeret tekst fundet til import.";
            return;
        }

        IsEditorExpanded = true;
        EditorText = normalized;
        CaretIndex = normalized.Length;
        StatusMessage = $"Importerede {normalized.Length} tegn fra {source}.";
    }

    public void InsertDictatedText(string text, string source)
    {
        var normalized = NormalizeDictatedText(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            StatusMessage = "Talegenkendelse gav ingen tekst at indsætte.";
            return;
        }

        EnsureEditorExpanded();
        var caretIndex = Math.Clamp(CaretIndex, 0, EditorText.Length);
        var prefix = EditorText[..caretIndex];
        var suffix = EditorText[caretIndex..];
        var leadingSpacing = prefix.Length > 0 && !char.IsWhiteSpace(prefix[^1])
            ? " "
            : string.Empty;
        var trailingSpacing = suffix.Length > 0 && !char.IsWhiteSpace(suffix[0])
            ? " "
            : string.Empty;
        var inserted = leadingSpacing + normalized + trailingSpacing;

        EditorText = prefix + inserted + suffix;
        CaretIndex = (prefix + inserted).Length;
        StatusMessage = $"Indsatte {normalized.Length} tegn fra {source}.";
    }

    public void HandleToolbarAction(string action)
    {
        StatusMessage = action switch
        {
            "wordList" => "Word list manager er planlagt til senere Windows-parity arbejde.",
            "import" => "Import af markering til editoren kommer med Windows selection adapters.",
            "textToSpeech" => "Text-to-speech er ikke porteret endnu i Windows-sporet.",
            "insights" => "Error insights bliver porteret i et senere Windows UI-sprint.",
            "settings" => "Settings-parity følger efter den primære toolbar/editor shell.",
            _ => StatusMessage
        };
    }

    public void ToggleAnalyzerColoring()
    {
        IsAnalyzerColoringEnabled = !IsAnalyzerColoringEnabled;
        PersistCurrentSettings();
        NotifyEditorSurfaceStateChanged();
        StatusMessage = IsAnalyzerColoringEnabled
            ? "Farvekodning er markeret som aktiv i Windows-shell'en."
            : "Farvekodning er markeret som inaktiv i Windows-shell'en.";
    }

    public void ToggleSemanticDiagnostics()
    {
        IsSemanticDiagnosticsEnabled = !IsSemanticDiagnosticsEnabled;
        PersistCurrentSettings();
        NotifyEditorSurfaceStateChanged();
        StatusMessage = IsSemanticDiagnosticsEnabled
            ? "Semantik-knappen er slået til som del af editor-parity baseline."
            : "Semantik-knappen er slået fra som del af editor-parity baseline.";
    }

    public void TogglePunctuationDiagnostics()
    {
        IsPunctuationDiagnosticsEnabled = !IsPunctuationDiagnosticsEnabled;
        PersistCurrentSettings();
        NotifyEditorSurfaceStateChanged();
        StatusMessage = IsPunctuationDiagnosticsEnabled
            ? "Tegnsætningsknappen er slået til som del af editor-parity baseline."
            : "Tegnsætningsknappen er slået fra som del af editor-parity baseline.";
    }

    public void RefreshSuggestionsPreview()
    {
        ScheduleSuggestionsRefresh();
        StatusMessage = "Forslags-preview opdateres fra WordSuggestorCore.";
    }

    public void SetStatusMessage(string message)
    {
        StatusMessage = message;
    }

    public void SetSpeechToTextListening(bool isListening, string statusMessage)
    {
        IsSpeechToTextListening = isListening;
        StatusMessage = statusMessage;
    }

    public void SetTextToSpeechSpeaking(bool isSpeaking, string statusMessage)
    {
        IsTextToSpeechSpeaking = isSpeaking;
        StatusMessage = statusMessage;
    }

    public ErrorInsightsSnapshot LoadInsightsSnapshot() => _insightsStore.LoadSnapshot();

    public AppSettingsSnapshot LoadSettingsSnapshot() => CaptureCurrentSettings(_settingsService.Load());

    public void ApplySettingsSnapshot(AppSettingsSnapshot settings)
    {
        _settingsService.Save(settings);

        var selectedLanguage = LanguageOptions.FirstOrDefault(option =>
            string.Equals(option.LanguageCode, settings.SelectedLanguageCode, StringComparison.OrdinalIgnoreCase));
        if (selectedLanguage is not null)
        {
            SelectedLanguageOption = selectedLanguage;
        }

        IsGlobalCaptureEnabled = settings.IsGlobalCaptureEnabled;
        IsAnalyzerColoringEnabled = settings.IsTextAnalyzerColoringEnabled;
        IsSemanticDiagnosticsEnabled = settings.IsSemanticDiagnosticsEnabled;
        IsPunctuationDiagnosticsEnabled = settings.IsPunctuationDiagnosticsEnabled;
        _isErrorTrackingEnabled = settings.IsErrorTrackingEnabled;
        SuggestionPlacementMode = ResolveSuggestionPlacementMode(settings.SuggestionPlacementMode);
        NotifyEditorSurfaceStateChanged();
        OnPropertyChanged(nameof(OverlaySupportSummary));
        StatusMessage = "Indstillinger er gemt og anvendt i Windows-sessionen.";
    }

    public void RecordBackspaceActivity()
    {
        if (_isErrorTrackingEnabled)
        {
            _insightsStore.RecordBackspace(SelectedLanguageOption.LanguageCode);
        }
    }

    public void RecordSentenceBoundary(string boundary)
    {
        if (_isErrorTrackingEnabled)
        {
            _insightsStore.RecordSentenceBoundary(SelectedLanguageOption.LanguageCode, boundary);
        }
    }

    public void SetSuggestionPlacementMode(SuggestionPlacementMode mode)
    {
        SuggestionPlacementMode = mode;
        PersistCurrentSettings();
        OnPropertyChanged(nameof(OverlaySupportSummary));
        StatusMessage = mode == SuggestionPlacementMode.FollowCaret
            ? "Ordforslagsboksen følger nu markøren, når caret-placering er tilgængelig."
            : "Ordforslagsboksen bruger nu statisk placering.";
    }

    public void ChangeSuggestionPage(int delta)
    {
        if (TotalSuggestionPages <= 1)
        {
            return;
        }

        var next = (CurrentSuggestionPage + delta) % TotalSuggestionPages;
        _currentSuggestionPage = next < 0 ? next + TotalSuggestionPages : next;
        UpdateSuggestionPageState();

        SelectedSuggestion = VisibleSuggestions.FirstOrDefault()?.Suggestion;
        OnPropertyChanged(nameof(OverlaySupportSummary));
        StatusMessage = $"Viser {SuggestionPageSummary.ToLowerInvariant()} i ordforslagsboksen.";
    }

    private void ExecuteAcceptSelectedSuggestion()
    {
        if (SelectedSuggestion is null)
        {
            return;
        }

        var acceptedSuggestion = SelectedSuggestion;
        var acceptedTerm = acceptedSuggestion.Term;
        var acceptedRankIndex = Suggestions.IndexOf(acceptedSuggestion);
        var acceptedRank = acceptedRankIndex >= 0 ? acceptedRankIndex + 1 : (int?)null;
        var typedToken = CurrentTokenAtCaret(EditorText, CaretIndex);
        var nextText = ReplaceActiveToken(EditorText, CaretIndex, acceptedTerm, out var nextCaretIndex);
        EditorText = nextText;
        CaretIndex = nextCaretIndex;
        if (_isErrorTrackingEnabled)
        {
            _insightsStore.RecordAcceptedSuggestion(
                typedToken,
                acceptedSuggestion,
                SelectedLanguageOption.LanguageCode,
                acceptedRank);
        }

        ClearSuggestionSession(keepOverlayVisible: true);
        StatusMessage = $"Indsatte '{acceptedTerm}'. Skriv videre for nye forslag.";
    }

    private void ClearSuggestionSession(bool keepOverlayVisible)
    {
        _suggestionCts?.Cancel();
        _suggestionCts?.Dispose();
        _suggestionCts = null;
        ClearSuggestionResults(keepOverlayVisible);
    }

    private void ClearSuggestionResults(bool keepOverlayVisible)
    {
        _isSuggestionOverlaySessionVisible = keepOverlayVisible;
        _currentSuggestionPage = 0;
        Suggestions.Clear();
        SelectedSuggestion = null;
        IsBusy = false;
        UpdateSuggestionPageState();
    }

    private void ScheduleSuggestionsRefresh()
    {
        _suggestionCts?.Cancel();
        _suggestionCts?.Dispose();
        _suggestionCts = new CancellationTokenSource();
        var token = _suggestionCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(220, token);
                await RefreshSuggestionsAsync(EditorText, token);
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    private async Task RefreshSuggestionsAsync(string text, CancellationToken cancellationToken)
    {
        if (text.Length == 0)
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                ClearSuggestionResults(keepOverlayVisible: false);
                StatusMessage = "Skriv i editoren for at hente live forslag fra WordSuggestorCore.";
            });
            return;
        }

        if (!HasActiveSuggestionToken(text))
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                ClearSuggestionResults(keepOverlayVisible: true);
                StatusMessage = "Ordforslagsboksen er klar til næste ord.";
            });
            return;
        }

        var selectedLanguage = _suggestionProvider.SelectedLanguage;
        if (!selectedLanguage.IsPackAvailable)
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                ClearSuggestionResults(keepOverlayVisible: true);
                StatusMessage = $"{selectedLanguage.DisplayName} er valgt, men sprogpakken mangler.";
            });
            return;
        }

        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            _isSuggestionOverlaySessionVisible = true;
            IsBusy = true;
            UpdateSuggestionPageState();
            StatusMessage = "Requesting suggestions from WordSuggestorCore...";
        });

        try
        {
            var suggestions = await _suggestionProvider.SuggestAsync(text, cancellationToken);

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                _isSuggestionOverlaySessionVisible = true;
                Suggestions.Clear();
                foreach (var suggestion in suggestions)
                {
                    Suggestions.Add(suggestion);
                }

                SelectedSuggestion = VisibleSuggestions.FirstOrDefault()?.Suggestion;
                StatusMessage = suggestions.Count == 0
                    ? "No suggestions returned for the current input."
                    : $"Received {suggestions.Count} suggestion(s) from WordSuggestorCore.";
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                ClearSuggestionResults(keepOverlayVisible: false);
                StatusMessage = $"Suggestion request failed: {ex.Message}";
            });
        }
        finally
        {
            await App.Current.Dispatcher.InvokeAsync(() => IsBusy = false);
        }
    }

    private void SuggestionsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var maxPageIndex = Math.Max(0, TotalSuggestionPages - 1);
        if (_currentSuggestionPage > maxPageIndex)
        {
            _currentSuggestionPage = maxPageIndex;
        }

        UpdateSuggestionPageState();
    }

    private void UpdateSuggestionPageState()
    {
        OnPropertyChanged(nameof(CurrentSuggestionPage));
        OnPropertyChanged(nameof(TotalSuggestionCount));
        OnPropertyChanged(nameof(TotalSuggestionPages));
        OnPropertyChanged(nameof(SuggestionPageSummary));
        OnPropertyChanged(nameof(SuggestionPanelCountSummary));
        OnPropertyChanged(nameof(VisibleSuggestions));
        OnPropertyChanged(nameof(HasSuggestions));
        OnPropertyChanged(nameof(ShouldShowSuggestionOverlay));
        OnPropertyChanged(nameof(OverlaySupportSummary));
    }

    private bool CanAcceptSelectedSuggestion() => SelectedSuggestion is not null;

    private static string CurrentTokenAtCaret(string text, int caretIndex)
    {
        var safeCaret = Math.Clamp(caretIndex, 0, text.Length);
        var start = safeCaret;

        while (start > 0 && IsTokenCharacter(text[start - 1]))
        {
            start--;
        }

        return text[start..safeCaret];
    }

    private static string ReplaceActiveToken(string text, int caretIndex, string replacement, out int nextCaretIndex)
    {
        var safeCaret = Math.Clamp(caretIndex, 0, text.Length);
        var start = safeCaret;
        var end = safeCaret;

        while (start > 0 && IsTokenCharacter(text[start - 1]))
        {
            start--;
        }

        while (end < text.Length && IsTokenCharacter(text[end]))
        {
            end++;
        }

        var prefix = text[..start];
        var suffix = text[end..];
        var spacing = suffix.StartsWith(' ') || suffix.StartsWith(Environment.NewLine) ? string.Empty : " ";
        var nextText = prefix + replacement + spacing + suffix;
        nextCaretIndex = (prefix + replacement + spacing).Length;
        return nextText;
    }

    private static bool IsTokenCharacter(char c) =>
        char.IsLetterOrDigit(c) || c is '\'' or '-';

    private static bool ShouldClearSuggestionsForBoundaryText(string text) =>
        text.Length == 0 || !HasActiveSuggestionToken(text);

    private static bool HasActiveSuggestionToken(string text) =>
        text.Length > 0 && IsTokenCharacter(text[^1]);

    private static string NormalizeImportedText(string text) =>
        text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Trim();

    private static string NormalizeDictatedText(string text) =>
        text
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();

    private AppSettingsSnapshot CaptureCurrentSettings(AppSettingsSnapshot settings)
    {
        var snapshot = settings.Clone();
        snapshot.IsGlobalCaptureEnabled = IsGlobalCaptureEnabled;
        snapshot.SelectedLanguageCode = SelectedLanguageOption.LanguageCode;
        snapshot.SuggestionPlacementMode = SuggestionPlacementMode == SuggestionPlacementMode.FollowCaret
            ? "followCaret"
            : "static";
        snapshot.IsTextAnalyzerColoringEnabled = IsAnalyzerColoringEnabled;
        snapshot.IsSemanticDiagnosticsEnabled = IsSemanticDiagnosticsEnabled;
        snapshot.IsPunctuationDiagnosticsEnabled = IsPunctuationDiagnosticsEnabled;
        snapshot.IsErrorTrackingEnabled = _isErrorTrackingEnabled;
        return snapshot;
    }

    private void PersistCurrentSettings() => _settingsService.Save(CaptureCurrentSettings(_settingsService.Load()));

    private static SuggestionPlacementMode ResolveSuggestionPlacementMode(string? value) =>
        string.Equals(value, "static", StringComparison.OrdinalIgnoreCase)
            ? SuggestionPlacementMode.Static
            : SuggestionPlacementMode.FollowCaret;

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void NotifyEditorMetricsChanged()
    {
        OnPropertyChanged(nameof(CharacterCount));
        OnPropertyChanged(nameof(WordCount));
        OnPropertyChanged(nameof(SpellingCount));
        OnPropertyChanged(nameof(GrammarCount));
        OnPropertyChanged(nameof(StatusMetrics));
        OnPropertyChanged(nameof(AnalyzerLegendMetrics));
    }

    private void NotifyEditorSurfaceStateChanged()
    {
        OnPropertyChanged(nameof(EditorReadinessSummary));
        OnPropertyChanged(nameof(AnalyzerToggleSummary));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

