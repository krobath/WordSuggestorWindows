using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class MainWindow : Window
{
    private const double CollapsedWidth = 560;
    private const double CollapsedHeight = 68;
    private const double ExpandedWidth = 900;
    private const double ExpandedHeight = 640;
    private const double OverlayVerticalGap = 10;
    private const double StaticOverlayHorizontalOffset = 118;
    private const double StaticOverlayVerticalOffset = 44;
    private readonly MainWindowViewModel _viewModel;
    private bool _isInitialPositionApplied;
    private Point? _manualOverlayTopLeft;
    private SuggestionOverlayWindow? _overlayWindow;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += OnLoaded;
        Closed += OnClosed;
        LocationChanged += OnWindowLocationOrSizeChanged;
        SizeChanged += OnWindowLocationOrSizeChanged;
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

        SyncOverlayVisibility();

        if (_viewModel.IsEditorExpanded)
        {
            RefocusEditor();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
        LocationChanged -= OnWindowLocationOrSizeChanged;
        SizeChanged -= OnWindowLocationOrSizeChanged;

        if (_overlayWindow is not null)
        {
            _overlayWindow.Close();
            _overlayWindow = null;
        }
    }

    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainWindowViewModel.IsEditorExpanded):
                ApplyShellSize();
                SyncOverlayVisibility();
                if (_viewModel.IsEditorExpanded)
                {
                    Dispatcher.BeginInvoke(RefocusEditor);
                }
                break;
            case nameof(MainWindowViewModel.ShouldShowSuggestionOverlay):
            case nameof(MainWindowViewModel.CurrentSuggestionPage):
            case nameof(MainWindowViewModel.VisibleSuggestions):
                SyncOverlayVisibility();
                break;
            case nameof(MainWindowViewModel.SuggestionPlacementMode):
                if (_viewModel.IsStaticPlacementMode)
                {
                    CaptureOrInitializeStaticOverlayPosition();
                }

                SyncOverlayVisibility();
                break;
        }
    }

    private void OnWindowLocationOrSizeChanged(object? sender, EventArgs e)
    {
        UpdateOverlayPosition();
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

    private void EditorTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateOverlayPosition();
    }

    private void EditorTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        _viewModel.CaretIndex = EditorTextBox.SelectionStart;
        UpdateOverlayPosition();
    }

    private void EditorTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (TryHandleControlDigitSuggestion(e) || TryHandleControlPaging(e))
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

    private bool TryHandleControlPaging(KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            return false;
        }

        if (e.Key == Key.Left)
        {
            _viewModel.ChangeSuggestionPage(-1);
            e.Handled = true;
            return true;
        }

        if (e.Key == Key.Right)
        {
            _viewModel.ChangeSuggestionPage(1);
            e.Handled = true;
            return true;
        }

        return false;
    }

    private void RefocusEditor()
    {
        EditorTextBox.Focus();
        EditorTextBox.SelectionStart = _viewModel.CaretIndex;
        EditorTextBox.SelectionLength = 0;
        UpdateOverlayPosition();
    }

    private void SyncOverlayVisibility()
    {
        if (!_viewModel.ShouldShowSuggestionOverlay)
        {
            HideOverlayWindow();
            return;
        }

        EnsureOverlayWindow();
        UpdateOverlayPosition();

        if (_overlayWindow is not null && !_overlayWindow.IsVisible)
        {
            _overlayWindow.Show();
        }
    }

    private void EnsureOverlayWindow()
    {
        if (_overlayWindow is not null)
        {
            return;
        }

        _overlayWindow = new SuggestionOverlayWindow(_viewModel)
        {
            Owner = this
        };
        _overlayWindow.ManualPlacementCommitted += OverlayWindowOnManualPlacementCommitted;
    }

    private void HideOverlayWindow()
    {
        if (_overlayWindow is not null && _overlayWindow.IsVisible)
        {
            _overlayWindow.Hide();
        }
    }

    private void UpdateOverlayPosition()
    {
        if (_overlayWindow is null || !_viewModel.ShouldShowSuggestionOverlay)
        {
            return;
        }

        EnsureOverlayWindow();

        var width = _overlayWindow.Width;
        var height = _overlayWindow.Height;
        double left;
        double top;

        if (_viewModel.IsStaticPlacementMode)
        {
            var staticTopLeft = ClampOverlayTopLeft(_manualOverlayTopLeft ?? ResolveDefaultStaticOverlayTopLeft(), width, height);
            _manualOverlayTopLeft = staticTopLeft;
            left = staticTopLeft.X;
            top = staticTopLeft.Y;
        }
        else
        {
            var anchor = ResolveOverlayAnchor();
            (left, top) = ResolveFollowCaretTopLeft(anchor, width, height);
        }

        _overlayWindow.Left = left;
        _overlayWindow.Top = top;
    }

    private Point ResolveOverlayAnchor()
    {
        if (_viewModel.IsFollowCaretPlacementMode &&
            TryGetCaretScreenRect(out var caretRect))
        {
            return new Point(caretRect.Left + (caretRect.Width / 2), caretRect.Bottom);
        }

        var staticOrigin = EditorTextBox.PointToScreen(new Point(StaticOverlayHorizontalOffset, StaticOverlayVerticalOffset));
        return new Point(staticOrigin.X, staticOrigin.Y);
    }

    private (double Left, double Top) ResolveFollowCaretTopLeft(Point anchor, double width, double height)
    {
        var workArea = SystemParameters.WorkArea;
        var left = anchor.X - (width / 2);
        var top = anchor.Y + OverlayVerticalGap;

        if (left < workArea.Left + 8)
        {
            left = workArea.Left + 8;
        }

        if (left + width > workArea.Right - 8)
        {
            left = workArea.Right - width - 8;
        }

        if (top + height > workArea.Bottom - 8)
        {
            top = Math.Max(workArea.Top + 8, anchor.Y - height - 18);
        }

        return (left, top);
    }

    private Point ResolveDefaultStaticOverlayTopLeft()
    {
        if (_overlayWindow is null)
        {
            return new Point(Left + 24, Top + 88);
        }

        var anchor = ResolveOverlayAnchor();
        var topLeft = ResolveFollowCaretTopLeft(anchor, _overlayWindow.Width, _overlayWindow.Height);
        return new Point(topLeft.Left, topLeft.Top);
    }

    private Point ClampOverlayTopLeft(Point candidate, double width, double height)
    {
        var workArea = SystemParameters.WorkArea;
        var left = candidate.X;
        var top = candidate.Y;

        if (left < workArea.Left + 8)
        {
            left = workArea.Left + 8;
        }

        if (left + width > workArea.Right - 8)
        {
            left = workArea.Right - width - 8;
        }

        if (top < workArea.Top + 8)
        {
            top = workArea.Top + 8;
        }

        if (top + height > workArea.Bottom - 8)
        {
            top = workArea.Bottom - height - 8;
        }

        return new Point(left, top);
    }

    private void CaptureOrInitializeStaticOverlayPosition()
    {
        if (_overlayWindow is not null && _overlayWindow.IsVisible)
        {
            _manualOverlayTopLeft = new Point(_overlayWindow.Left, _overlayWindow.Top);
            return;
        }

        _manualOverlayTopLeft ??= ResolveDefaultStaticOverlayTopLeft();
    }

    private void OverlayWindowOnManualPlacementCommitted(object? sender, Point topLeft)
    {
        if (_overlayWindow is null)
        {
            return;
        }

        _manualOverlayTopLeft = ClampOverlayTopLeft(topLeft, _overlayWindow.Width, _overlayWindow.Height);
        _overlayWindow.Left = _manualOverlayTopLeft.Value.X;
        _overlayWindow.Top = _manualOverlayTopLeft.Value.Y;
        _viewModel.SetStatusMessage("Ordforslagsboksen blev flyttet i statisk placering.");
    }

    private bool TryGetCaretScreenRect(out Rect caretRect)
    {
        caretRect = Rect.Empty;

        if (!EditorTextBox.IsLoaded)
        {
            return false;
        }

        var caretIndex = Math.Clamp(EditorTextBox.CaretIndex, 0, EditorTextBox.Text.Length);
        var candidateRects = new[]
        {
            EditorTextBox.GetRectFromCharacterIndex(caretIndex, true),
            EditorTextBox.GetRectFromCharacterIndex(caretIndex, false)
        };

        var rect = candidateRects.FirstOrDefault(r => !r.IsEmpty && r.Width >= 0 && r.Height >= 0);
        if (rect.IsEmpty)
        {
            return false;
        }

        var topLeft = EditorTextBox.PointToScreen(new Point(rect.Left, rect.Top));
        caretRect = new Rect(topLeft.X, topLeft.Y, Math.Max(1, rect.Width), Math.Max(20, rect.Height));
        return true;
    }
}
