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
    private readonly ISuggestionProvider _suggestionProvider;
    private readonly RelayCommand _acceptSelectedSuggestionCommand;
    private CancellationTokenSource? _suggestionCts;
    private string _editorText = string.Empty;
    private SuggestionItem? _selectedSuggestion;
    private string _statusMessage;
    private bool _isBusy;
    private int _caretIndex;
    private bool _isEditorExpanded;
    private bool _isGlobalCaptureEnabled = true;
    private string _selectedLanguageOption = "🇩🇰 Dansk";
    private bool _isAnalyzerColoringEnabled = true;
    private bool _isSemanticDiagnosticsEnabled;
    private bool _isPunctuationDiagnosticsEnabled;

    public MainWindowViewModel(ISuggestionProvider suggestionProvider, string? initialEditorText = null)
    {
        _suggestionProvider = suggestionProvider;
        ProviderDescription = suggestionProvider.ProviderDescription;
        _editorText = initialEditorText ?? string.Empty;
        _caretIndex = _editorText.Length;
        _statusMessage = string.IsNullOrWhiteSpace(initialEditorText)
            ? "Windows toolbar shell klar. Udvid editoren for at skrive og hente forslag."
            : "Startup sample loaded. Suggestions will refresh automatically.";
        LanguageOptions = ["🇩🇰 Dansk"];
        Suggestions = [];
        Suggestions.CollectionChanged += SuggestionsOnCollectionChanged;
        _acceptSelectedSuggestionCommand = new RelayCommand(ExecuteAcceptSelectedSuggestion, CanAcceptSelectedSuggestion);

        if (!string.IsNullOrWhiteSpace(_editorText))
        {
            _isEditorExpanded = true;
            ScheduleSuggestionsRefresh();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SuggestionItem> Suggestions { get; }

    public ICommand AcceptSelectedSuggestionCommand => _acceptSelectedSuggestionCommand;

    public IReadOnlyList<string> LanguageOptions { get; }

    public string ProviderDescription { get; }

    public string EditorText
    {
        get => _editorText;
        set
        {
            if (SetProperty(ref _editorText, value))
            {
                NotifyEditorMetricsChanged();
                ScheduleSuggestionsRefresh();
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

    public bool HasSuggestions => Suggestions.Count > 0;

    public string SuggestionPreviewCaption => HasSuggestions
        ? $"Live forslag ({Suggestions.Count})"
        : "Live forslag";

    public int CharacterCount => EditorText.Length;

    public int WordCount => EditorText
        .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
        .Length;

    public int SpellingCount => 0;

    public int GrammarCount => 0;

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

    public bool AcceptSuggestionAtIndex(int index)
    {
        if (index < 0 || index >= Suggestions.Count)
        {
            return false;
        }

        SelectedSuggestion = Suggestions[index];
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
        StatusMessage = IsAnalyzerColoringEnabled
            ? "Farvekodning er markeret som aktiv i Windows-shell'en."
            : "Farvekodning er markeret som inaktiv i Windows-shell'en.";
    }

    public void ToggleSemanticDiagnostics()
    {
        IsSemanticDiagnosticsEnabled = !IsSemanticDiagnosticsEnabled;
        StatusMessage = IsSemanticDiagnosticsEnabled
            ? "Semantik-knappen er slået til som del af editor-parity baseline."
            : "Semantik-knappen er slået fra som del af editor-parity baseline.";
    }

    public void TogglePunctuationDiagnostics()
    {
        IsPunctuationDiagnosticsEnabled = !IsPunctuationDiagnosticsEnabled;
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

    private void ExecuteAcceptSelectedSuggestion()
    {
        if (SelectedSuggestion is null)
        {
            return;
        }

        var nextText = ReplaceActiveToken(EditorText, CaretIndex, SelectedSuggestion.Term, out var nextCaretIndex);
        EditorText = nextText;
        CaretIndex = nextCaretIndex;
        StatusMessage = $"Accepted suggestion '{SelectedSuggestion.Term}'.";
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
        if (string.IsNullOrWhiteSpace(text))
        {
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                Suggestions.Clear();
                SelectedSuggestion = null;
                IsBusy = false;
                StatusMessage = "Skriv i editoren for at hente live forslag fra WordSuggestorCore.";
            });
            return;
        }

        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            IsBusy = true;
            StatusMessage = "Requesting suggestions from WordSuggestorCore...";
        });

        try
        {
            var suggestions = await _suggestionProvider.SuggestAsync(text, cancellationToken);

            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                Suggestions.Clear();
                foreach (var suggestion in suggestions)
                {
                    Suggestions.Add(suggestion);
                }

                SelectedSuggestion = Suggestions.FirstOrDefault();
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
                Suggestions.Clear();
                SelectedSuggestion = null;
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
        OnPropertyChanged(nameof(HasSuggestions));
        OnPropertyChanged(nameof(SuggestionPreviewCaption));
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
        var spacing = suffix.StartsWith(' ') || suffix.StartsWith(Environment.NewLine) || suffix.Length == 0 ? string.Empty : " ";
        var nextText = prefix + replacement + spacing + suffix;
        nextCaretIndex = (prefix + replacement + spacing).Length;
        return nextText;
    }

    private static bool IsTokenCharacter(char c) =>
        char.IsLetterOrDigit(c) || c is '\'' or '-';

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
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
