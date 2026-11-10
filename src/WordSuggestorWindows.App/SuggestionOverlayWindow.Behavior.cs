using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class SuggestionOverlayWindow
{
    private readonly DispatcherTimer _idleTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private long _lastSuggestionActivity;
    private CancellationTokenSource? _hoverCancellation;
    public event EventHandler<string>? HoverSpeechRequested;
    public event EventHandler? HoverSpeechCancelled;
    private void InitializeSuggestionBehavior()
    {
        _viewModel.PropertyChanged += SuggestionBehaviorOnChanged;
        _viewModel.SettingsChanged += SuggestionBehaviorOnSettingsChanged;
        IsVisibleChanged += SuggestionVisibilityChanged;
        MouseMove += SuggestionMouseActivity;
        _idleTimer.Tick += SuggestionIdleTick;
        ResetSuggestionIdle(); _idleTimer.Start();
    }
    private void DisposeSuggestionBehavior()
    {
        _idleTimer.Stop(); _idleTimer.Tick -= SuggestionIdleTick;
        _viewModel.PropertyChanged -= SuggestionBehaviorOnChanged;
        _viewModel.SettingsChanged -= SuggestionBehaviorOnSettingsChanged;
        IsVisibleChanged -= SuggestionVisibilityChanged; MouseMove -= SuggestionMouseActivity;
        CancelHoverSpeech();
    }
    private void SuggestionBehaviorOnChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.EditorText) or nameof(MainWindowViewModel.ExternalSuggestionToken) or
            nameof(MainWindowViewModel.VisibleSuggestions) or nameof(MainWindowViewModel.VisiblePrimarySuggestions) or nameof(MainWindowViewModel.VisibleSecondarySuggestions))
        { CancelHoverSpeech(); ResetSuggestionIdle(); }
    }
    private void SuggestionBehaviorOnSettingsChanged(object? sender, AppSettingsSnapshot settings) { CancelHoverSpeech(); ResetSuggestionIdle(); }
    private void SuggestionVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e) { CancelHoverSpeech(); ResetSuggestionIdle(); }
    private void SuggestionMouseActivity(object sender, MouseEventArgs e) => ResetSuggestionIdle();
    private void ResetSuggestionIdle()
    {
        _lastSuggestionActivity = Environment.TickCount64; Opacity = 1;
        _viewModel.SetSuggestionInteractionEnabled(true);
    }
    private void SuggestionIdleTick(object? sender, EventArgs e)
    {
        if (!IsVisible) return;
        var seconds = _viewModel.LoadSettingsSnapshot().Suggestions.IdleFadeSeconds;
        if (seconds >= 31) return;
        var elapsed = Environment.TickCount64 - _lastSuggestionActivity - seconds * 1000L;
        if (elapsed < 0) return;
        _viewModel.SetSuggestionInteractionEnabled(false);
        CancelHoverSpeech();
        Opacity = Math.Clamp(1 - elapsed / 3000.0, 0, 1);
        if (elapsed >= 3000) _viewModel.HideIdleSuggestions();
    }
    private void CancelHoverSpeech()
    {
        _hoverCancellation?.Cancel(); _hoverCancellation?.Dispose(); _hoverCancellation = null;
        HoverSpeechCancelled?.Invoke(this, EventArgs.Empty);
    }
    private async void SuggestionRow_OnMouseEnter(object sender, MouseEventArgs e)
    {
        CancelHoverSpeech(); ResetSuggestionIdle();
        if (!_viewModel.LoadSettingsSnapshot().Suggestions.SpeakOnHover || sender is not Border { DataContext: SuggestionOverlayEntry entry }) return;
        var scope = _viewModel.SuggestionScope;
        var cancellation = new CancellationTokenSource(); _hoverCancellation = cancellation;
        var token = cancellation.Token;
        try
        {
            await Task.Delay(300, token);
            if (!token.IsCancellationRequested && IsVisible && ((Border)sender).IsMouseOver && _viewModel.SuggestionScope == scope && _viewModel.LoadSettingsSnapshot().Suggestions.SpeakOnHover)
                HoverSpeechRequested?.Invoke(this, entry.Suggestion.Term);
        }
        catch (OperationCanceledException) { }
    }
    private void SuggestionRow_OnMouseLeave(object sender, MouseEventArgs e) => CancelHoverSpeech();
    private void DomainPrevious_OnClick(object sender, RoutedEventArgs e) { if (sender is Button { Tag: string lane }) _viewModel.ChangeLanePage(int.Parse(lane), -1); }
    private void DomainNext_OnClick(object sender, RoutedEventArgs e) { if (sender is Button { Tag: string lane }) _viewModel.ChangeLanePage(int.Parse(lane), 1); }
}
