using System.Windows;
using System.Windows.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class SuggestionOverlayWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public SuggestionOverlayWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
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

    private void SuggestionRow_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SuggestionOverlayEntry entry })
        {
            _viewModel.AcceptSuggestion(entry.Suggestion);
        }
    }
}
