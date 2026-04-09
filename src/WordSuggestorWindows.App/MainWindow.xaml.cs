using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class MainWindow : Window
{
    private const double CollapsedWidth = 560;
    private const double CollapsedHeight = 68;
    private const double ExpandedWidth = 900;
    private const double ExpandedHeight = 640;
    private readonly MainWindowViewModel _viewModel;
    private bool _isInitialPositionApplied;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;
        _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!_isInitialPositionApplied)
        {
            PositionAtTopCenter();
            ApplyShellSize(force: true);
            _isInitialPositionApplied = true;
        }

        if (_viewModel.IsEditorExpanded)
        {
            RefocusEditor();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsEditorExpanded))
        {
            ApplyShellSize();
            if (_viewModel.IsEditorExpanded)
            {
                Dispatcher.BeginInvoke(RefocusEditor);
            }
        }
    }

    private void PositionAtTopCenter()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + ((workArea.Width - Width) / 2);
        Top = workArea.Top + 18;
    }

    private void ApplyShellSize(bool force = false)
    {
        var nextWidth = _viewModel.IsEditorExpanded ? ExpandedWidth : CollapsedWidth;
        var nextHeight = _viewModel.IsEditorExpanded ? ExpandedHeight : CollapsedHeight;

        if (!force &&
            Math.Abs(Width - nextWidth) < 0.5 &&
            Math.Abs(Height - nextHeight) < 0.5)
        {
            return;
        }

        var topEdge = Top;
        var midX = Left + (Width / 2);

        Width = nextWidth;
        Height = nextHeight;
        Left = midX - (nextWidth / 2);
        Top = topEdge;

        ClampToWorkArea();
    }

    private void ClampToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        if (Left < workArea.Left)
        {
            Left = workArea.Left;
        }

        if (Top < workArea.Top)
        {
            Top = workArea.Top;
        }

        if (Left + Width > workArea.Right)
        {
            Left = Math.Max(workArea.Left, workArea.Right - Width);
        }

        if (Top + Height > workArea.Bottom)
        {
            Top = Math.Max(workArea.Top, workArea.Bottom - Height);
        }
    }

    private void ToolbarSurface_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || IsInteractiveElement(e.OriginalSource as DependencyObject))
        {
            return;
        }

        DragMove();
    }

    private static bool IsInteractiveElement(DependencyObject? origin)
    {
        var current = origin;
        while (current is not null)
        {
            if (current is ButtonBase or ComboBox or TextBox)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private void ExpandCollapseButton_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleEditorExpanded();
    }

    private void ToolbarAction_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string action })
        {
            _viewModel.HandleToolbarAction(action);
        }
    }

    private void EditorCommand_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string action })
        {
            return;
        }

        switch (action)
        {
            case "cut":
                ApplicationCommands.Cut.Execute(null, EditorTextBox);
                _viewModel.SetStatusMessage("Klip udført i editoren.");
                break;
            case "copy":
                ApplicationCommands.Copy.Execute(null, EditorTextBox);
                _viewModel.SetStatusMessage("Kopiér udført i editoren.");
                break;
            case "paste":
                ApplicationCommands.Paste.Execute(null, EditorTextBox);
                _viewModel.SetStatusMessage("Indsæt udført i editoren.");
                break;
            case "coloring":
                _viewModel.ToggleAnalyzerColoring();
                break;
            case "semantic":
                _viewModel.ToggleSemanticDiagnostics();
                break;
            case "punctuation":
                _viewModel.TogglePunctuationDiagnostics();
                break;
            case "refreshSuggestions":
                _viewModel.RefreshSuggestionsPreview();
                break;
        }

        RefocusEditor();
    }

    private void SuggestionChip_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: SuggestionItem suggestion } &&
            _viewModel.AcceptSuggestion(suggestion))
        {
            RefocusEditor();
        }
    }

    private void EditorTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.CaretIndex = EditorTextBox.SelectionStart;
    }

    private void EditorTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (TryHandleControlDigitSuggestion(e))
        {
            return;
        }

        if (e.Key == Key.Tab && _viewModel.AcceptSelectedSuggestion())
        {
            RefocusEditor();
            e.Handled = true;
        }
    }

    private bool TryHandleControlDigitSuggestion(KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            return false;
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

        if (index < 0 || !_viewModel.AcceptSuggestionAtIndex(index))
        {
            return false;
        }

        RefocusEditor();
        e.Handled = true;
        return true;
    }

    private void RefocusEditor()
    {
        EditorTextBox.Focus();
        EditorTextBox.SelectionStart = _viewModel.CaretIndex;
        EditorTextBox.SelectionLength = 0;
    }
}
