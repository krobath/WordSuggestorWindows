using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class InternalEditorWindow : Window
{
    private readonly MainWindow _controller;
    private readonly MainWindowViewModel _viewModel;
    private bool _ready;
    private bool _shutdown;

    public InternalEditorWindow(MainWindow controller, MainWindowViewModel viewModel)
    {
        _controller = controller;
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = viewModel;
        _ready = true;
        Activated += (_, _) => UpdateFocus();
        Deactivated += (_, _) => _viewModel.SetLocalEditorActive(false);
        GotKeyboardFocus += (_, _) => UpdateFocus();
        LostKeyboardFocus += (_, _) => UpdateFocus();
        Closing += OnClosing;
    }

    internal RichTextBox TextEditor => EditorTextBox;

    public void ShowEditor()
    {
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        EditorTextBox.Focus();
        UpdateFocus();
    }

    internal void CloseForShutdown() { _shutdown = true; Close(); }

    private void UpdateFocus() => _viewModel.SetLocalEditorActive(IsActive && EditorTextBox.IsKeyboardFocusWithin);

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_shutdown) return;
        e.Cancel = true;
        Hide();
        _viewModel.SetLocalEditorActive(false);
        if (_viewModel.IsEditorExpanded) _viewModel.ToggleEditorExpanded();
    }

    private void EditorCommand_OnClick(object sender, RoutedEventArgs e) => _controller.EditorCommand_OnClick(sender, e);
    private void ToolbarAction_OnClick(object sender, RoutedEventArgs e) => _controller.ToolbarAction_OnClick(sender, e);
    private void Quit_OnClick(object sender, RoutedEventArgs e) => _controller.Close();
    private void EditorTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_ready) _controller.EditorTextBox_OnTextChanged(sender, e);
    }
    private void EditorTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_ready) _controller.EditorTextBox_OnSelectionChanged(sender, e);
    }
    private void EditorTextBox_OnPreviewKeyDown(object sender, KeyEventArgs e) => _controller.EditorTextBox_OnPreviewKeyDown(sender, e);
    private void EditorWindow_OnPreviewKeyDown(object sender, KeyEventArgs e) => _controller.MainWindow_OnPreviewKeyDown(sender, e);
}
