using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class SuggestionOverlayWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly OverlaySpeechService _speechService = new();

    public event EventHandler<Point>? ManualPlacementCommitted;

    public SuggestionOverlayWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Closed += OnClosed;
    }

    private void StaticPlacementButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SetSuggestionPlacementMode(SuggestionPlacementMode.Static);
    }

    private void FollowCaretButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.SetSuggestionPlacementMode(SuggestionPlacementMode.FollowCaret);
    }

    private void PreviousPageButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ChangeSuggestionPage(-1);
    }

    private void NextPageButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ChangeSuggestionPage(1);
    }

    private void DragSurface_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_viewModel.IsStaticPlacementMode ||
            e.ChangedButton != MouseButton.Left ||
            IsInteractiveElement(e.OriginalSource as DependencyObject))
        {
            return;
        }

        DragMove();
        ManualPlacementCommitted?.Invoke(this, new Point(Left, Top));
    }

    private void SuggestionRow_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsInteractiveElement(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (sender is Border { DataContext: SuggestionOverlayEntry entry })
        {
            _viewModel.AcceptSuggestion(entry.Suggestion);
        }
    }

    private void SuggestionOverlayWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            return;
        }

        if (e.Key == Key.Left)
        {
            _viewModel.ChangeSuggestionPage(-1);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Right)
        {
            _viewModel.ChangeSuggestionPage(1);
            e.Handled = true;
            return;
        }

        var index = e.Key switch
        {
            Key.D1 or Key.NumPad1 => 0,
            Key.D2 or Key.NumPad2 => 1,
            Key.D3 or Key.NumPad3 => 2,
            Key.D4 or Key.NumPad4 => 3,
            Key.D5 or Key.NumPad5 => 4,
            Key.D6 or Key.NumPad6 => 5,
            Key.D7 or Key.NumPad7 => 6,
            Key.D8 or Key.NumPad8 => 7,
            Key.D9 or Key.NumPad9 => 8,
            Key.D0 or Key.NumPad0 => 9,
            _ => -1,
        };

        if (index >= 0 && _viewModel.AcceptSuggestionAtIndex(index))
        {
            e.Handled = true;
        }
    }

    private void SpeakButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SuggestionOverlayEntry entry })
        {
            _speechService.Speak(entry.Suggestion.Term);
            _viewModel.SetStatusMessage($"Læser '{entry.Suggestion.Term}' op via Windows TTS.");
        }
    }

    private void InfoButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: SuggestionOverlayEntry entry } button)
        {
            return;
        }

        var infoPanel = new StackPanel
        {
            Margin = new Thickness(4)
        };

        infoPanel.Children.Add(new TextBlock
        {
            Text = entry.Suggestion.Term,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });

        foreach (var line in entry.InfoSummary.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            infoPanel.Children.Add(new TextBlock
            {
                Text = line,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 2),
                MaxWidth = 220
            });
        }

        var menu = new ContextMenu
        {
            Placement = PlacementMode.Left,
            PlacementTarget = button,
            StaysOpen = false,
            HasDropShadow = true
        };

        menu.Items.Add(new MenuItem
        {
            Header = infoPanel,
            StaysOpenOnClick = true
        });

        menu.IsOpen = true;
        _viewModel.SetStatusMessage($"Viser ordinfo for '{entry.Suggestion.Term}'.");
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        _speechService.Dispose();
    }

    private static bool IsInteractiveElement(DependencyObject? origin)
    {
        var current = origin;
        while (current is not null)
        {
            if (current is ButtonBase or ScrollBar)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
