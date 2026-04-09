using System.Windows;
using System.Windows.Input;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private void EditorTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.CaretIndex = EditorTextBox.SelectionStart;
    }

    private void EditorTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab && _viewModel.AcceptSelectedSuggestion())
        {
            EditorTextBox.Focus();
            EditorTextBox.SelectionStart = _viewModel.CaretIndex;
            EditorTextBox.SelectionLength = 0;
            e.Handled = true;
        }
    }

    private void SuggestionList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        AcceptSuggestionAndRefocusEditor();
    }

    private void AcceptSelectedSuggestion_OnClick(object sender, RoutedEventArgs e)
    {
        AcceptSuggestionAndRefocusEditor();
    }

    private void AcceptSuggestionAndRefocusEditor()
    {
        if (!_viewModel.AcceptSelectedSuggestion())
        {
            return;
        }

        EditorTextBox.Focus();
        EditorTextBox.SelectionStart = _viewModel.CaretIndex;
        EditorTextBox.SelectionLength = 0;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EditorTextBox.Focus();
        EditorTextBox.SelectionStart = _viewModel.CaretIndex;
        EditorTextBox.SelectionLength = 0;
    }
}
