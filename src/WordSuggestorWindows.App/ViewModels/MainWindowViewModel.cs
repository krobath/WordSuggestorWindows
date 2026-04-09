using System.Collections.ObjectModel;
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

    public MainWindowViewModel(ISuggestionProvider suggestionProvider)
    {
        _suggestionProvider = suggestionProvider;
        ProviderDescription = suggestionProvider.ProviderDescription;
        _statusMessage = "Type in the editor to request local suggestions from WordSuggestorCore.";
        Suggestions = [];
        _acceptSelectedSuggestionCommand = new RelayCommand(ExecuteAcceptSelectedSuggestion, CanAcceptSelectedSuggestion);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<SuggestionItem> Suggestions { get; }

    public ICommand AcceptSelectedSuggestionCommand => _acceptSelectedSuggestionCommand;

    public string ProviderDescription { get; }

    public string EditorText
    {
        get => _editorText;
        set
        {
            if (SetProperty(ref _editorText, value))
            {
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

    public bool AcceptSelectedSuggestion()
    {
        if (SelectedSuggestion is null)
        {
            return false;
        }

        ExecuteAcceptSelectedSuggestion();
        return true;
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
                StatusMessage = "Type in the editor to request local suggestions from WordSuggestorCore.";
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
