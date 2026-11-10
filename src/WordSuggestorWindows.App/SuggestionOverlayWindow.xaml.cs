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
    public event EventHandler<string>? SpeechRequested;

    public event EventHandler<Point>? ManualPlacementCommitted;

    public SuggestionOverlayWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        NonActivatingWindow.Apply(this);
        _viewModel = viewModel;
        DataContext = viewModel;
        Closed += OnClosed;
        InitializeSuggestionBehavior();
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
        ManualPlacementCommitted?.Invoke(this, ScreenPlacement.TopLeft(this));
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

    private void AcceptButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SuggestionOverlayEntry entry })
            _viewModel.AcceptSuggestion(entry.Suggestion);
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
            SpeechRequested?.Invoke(this, entry.Suggestion.Term);
        }
    }

    private async void InfoButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: SuggestionOverlayEntry entry } button)
        {
            return;
        }

        var infoPanel = new StackPanel
        {
            Margin = new Thickness(4)
        };
        var scope = _viewModel.SuggestionScope;
        var language = _viewModel.SelectedLanguageOption.LanguageCode;
        CoreWordInfo? wordInfo = null;
        var preferences = _viewModel.LoadSettingsSnapshot().Suggestions;
        if (preferences.WordForms && _viewModel.CoreSettings is { } adapter)
        {
            try
            {
                var caps = await adapter.GetCapabilitiesAsync();
                if (caps.WordFamilies || caps.MorphologicalAnalyses)
                {
                    var result = await adapter.GetWordInfoAsync(entry.Suggestion.Term, scope, caps.WordFamilies,
                        spacingMode: caps.CommitPlans ? preferences.Spacing : null, suffix: _viewModel.CurrentSuggestionSuffix);
                    if (result.IsCurrent(scope, language)) wordInfo = result.Value;
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException or System.IO.IOException or NotSupportedException) { }
        }
        if (!IsVisible || _viewModel.SuggestionScope != scope || _viewModel.SelectedLanguageOption.LanguageCode != language) return;

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

        if (wordInfo is not null)
        {
            foreach (var analysis in wordInfo.Analyses)
                menu.Items.Add(new MenuItem { Header = analysis.WordClass + " · " + analysis.Gram, IsEnabled = false });
            foreach (var family in wordInfo.Families)
            {
                var item = new MenuItem { Header = family.Term + " · " + family.Gram };
                item.Click += (_, _) =>
                {
                    if (_viewModel.SuggestionScope == scope && _viewModel.SelectedLanguageOption.LanguageCode == language)
                        _viewModel.AcceptFamilyForm(entry.Suggestion, family);
                };
                menu.Items.Add(item);
            }
        }

        menu.IsOpen = true;
        _viewModel.SetStatusMessage($"Viser ordinfo for '{entry.Suggestion.Term}'.");
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        Closed -= OnClosed;
        DisposeSuggestionBehavior();
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
