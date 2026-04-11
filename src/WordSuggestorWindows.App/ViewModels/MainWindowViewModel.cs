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
    private string _selectedLanguageOption = "DA";
    private bool _isAnalyzerColoringEnabled = true;
    private bool _isSemanticDiagnosticsEnabled;
    private bool _isPunctuationDiagnosticsEnabled;
    private int _currentSuggestionPage;
    private SuggestionPlacementMode _suggestionPlacementMode = SuggestionPlacementMode.FollowCaret;

    public MainWindowViewModel(ISuggestionProvider suggestionProvider, string? initialEditorText = null)
    {
        _suggestionProvider = suggestionProvider;
        ProviderDescription = suggestionProvider.ProviderDescription;
        _editorText = initialEditorText ?? string.Empty;
        _caretIndex = _editorText.Length;
        _statusMessage = string.IsNullOrWhiteSpace(initialEditorText)
            ? "Windows toolbar shell klar. Udvid editoren for at skrive og hente forslag."
            : "Startuptekst er klar. Åbn editoren for at vise og redigere teksten.";
        LanguageOptions = ["DA"];
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

    public IReadOnlyList<string> LanguageOptions { get; }

    public string ProviderDescription { get; }

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
                StatusMessage = value
                    ? "Global forslag er slået til. Cross-app integration kommer i WSA-RT-003."
                    : "Global forslag er slået fra i Windows-shell'en.";
            }
        }
    }

    public string SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set
        {
            if (SetProperty(ref _selectedLanguageOption, value))
            {
                StatusMessage = "Dansk er aktivt sprog i den nuværende Windows-baseline.";
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

    public void HandleToolbarAction(string action)
    {
        StatusMessage = action switch
        {
            "wordList" => "Word list manager er planlagt til senere Windows-parity arbejde.",
            "import" => "Import af markering til editoren kommer med Windows selection adapters.",
            "ocr" => "OCR er endnu ikke porteret til Windows.",
            "speechToText" => "Speech-to-text er ikke porteret endnu i Windows-sporet.",
            "textToSpeech" => "Text-to-speech er ikke porteret endnu i Windows-sporet.",
            "insights" => "Error insights bliver porteret i et senere Windows UI-sprint.",
            "settings" => "Settings-parity følger efter den primære toolbar/editor shell.",
            _ => StatusMessage
        };
    }

    public void ToggleAnalyzerColoring()
    {
        IsAnalyzerColoringEnabled = !IsAnalyzerColoringEnabled;
        NotifyEditorSurfaceStateChanged();
        StatusMessage = IsAnalyzerColoringEnabled
            ? "Farvekodning er markeret som aktiv i Windows-shell'en."
            : "Farvekodning er markeret som inaktiv i Windows-shell'en.";
    }

    public void ToggleSemanticDiagnostics()
    {
        IsSemanticDiagnosticsEnabled = !IsSemanticDiagnosticsEnabled;
        NotifyEditorSurfaceStateChanged();
        StatusMessage = IsSemanticDiagnosticsEnabled
            ? "Semantik-knappen er slået til som del af editor-parity baseline."
            : "Semantik-knappen er slået fra som del af editor-parity baseline.";
    }

    public void TogglePunctuationDiagnostics()
    {
        IsPunctuationDiagnosticsEnabled = !IsPunctuationDiagnosticsEnabled;
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

    public void SetSuggestionPlacementMode(SuggestionPlacementMode mode)
    {
        SuggestionPlacementMode = mode;
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

        var acceptedTerm = SelectedSuggestion.Term;
        var nextText = ReplaceActiveToken(EditorText, CaretIndex, acceptedTerm, out var nextCaretIndex);
        EditorText = nextText;
        CaretIndex = nextCaretIndex;
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

