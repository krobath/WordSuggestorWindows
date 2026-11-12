using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class InternalEditorWindow : Window
{
    private readonly MainWindow _controller;
    private readonly MainWindowViewModel _viewModel;
    private readonly EditorWorkspace _workspace;
    private readonly Dictionary<Guid, TabItem> _views = [];
    private RichTextBox _activeEditor = null!;
    private EditorDocumentTab _displayedDocument = null!;
    private bool _synchronizingTabs;
    private bool _ready;
    private bool _shutdown;

    public InternalEditorWindow(MainWindow controller, MainWindowViewModel viewModel, EditorWorkspace workspace)
    {
        _controller = controller;
        _viewModel = viewModel;
        _workspace = workspace;
        InitializeComponent();
        DataContext = viewModel;
        SelectDocument(workspace.Active);
        _ready = true;
        Activated += (_, _) => UpdateFocus();
        Deactivated += (_, _) => _viewModel.SetLocalEditorActive(false);
        GotKeyboardFocus += (_, _) => UpdateFocus();
        LostKeyboardFocus += (_, _) => UpdateFocus();
        Closing += OnClosing;
    }

    internal RichTextBox TextEditor => _activeEditor;
    internal EditorDocumentTab DisplayedDocument => _displayedDocument;

    internal void SynchronizeTabs()
    {
        _synchronizingTabs = true;
        try
        {
            foreach (var removed in _views.Keys.Where(id => !_workspace.Tabs.Any(t => t.Id == id)).ToArray())
            {
                DocumentTabs.Items.Remove(_views[removed]); _views.Remove(removed);
            }
            foreach (var document in _workspace.Tabs)
            {
                if (_views.ContainsKey(document.Id)) continue;
                var editor = new RichTextBox
                {
                    Padding = new Thickness(14), BorderThickness = new Thickness(0), Background = Brushes.Transparent,
                    FontSize = 20, AcceptsTab = false, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Document = new FlowDocument(new Paragraph()) { PagePadding = new Thickness(0) }
                };
                editor.IsUndoEnabled = false;
                new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text = document.Text;
                foreach (var paragraph in editor.Document.Blocks.OfType<Paragraph>()) paragraph.Margin = new Thickness(0);
                editor.IsUndoEnabled = true;
                editor.TextChanged += EditorTextBox_OnTextChanged;
                editor.SelectionChanged += EditorTextBox_OnSelectionChanged;
                editor.PreviewKeyDown += EditorTextBox_OnPreviewKeyDown;
                AutomationProperties.SetAutomationId(editor, "InternalEditor");
                AutomationProperties.SetName(editor, "Dokumenttekst");
                var close = new Button { Content = "×", Padding = new Thickness(5, 0, 5, 0), Margin = new Thickness(8, 0, 0, 0), ToolTip = "Luk fane", MinWidth = 24 };
                close.SetBinding(AutomationProperties.NameProperty, new Binding(nameof(document.Title)) { Source = document, StringFormat = "Luk {0}" });
                close.Click += (_, e) => { e.Handled = true; _controller.CloseEditorTab(document); };
                var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center, MaxWidth = 220, TextTrimming = TextTrimming.CharacterEllipsis };
                label.SetBinding(TextBlock.TextProperty, new Binding(nameof(document.Header)) { Source = document });
                var header = new DockPanel { Margin = new Thickness(4, 2, 4, 2) };
                DockPanel.SetDock(close, Dock.Right); header.Children.Add(close); header.Children.Add(label);
                var tab = new TabItem { Header = header, Content = editor, Tag = document };
                tab.SetBinding(AutomationProperties.NameProperty, new Binding(nameof(document.AccessibleName)) { Source = document });
                AutomationProperties.SetAutomationId(tab, "EditorTab_" + document.Id.ToString("N"));
                _views.Add(document.Id, tab); DocumentTabs.Items.Add(tab);
            }
        }
        finally { _synchronizingTabs = false; }
    }

    internal void SelectDocument(EditorDocumentTab document)
    {
        SynchronizeTabs();
        _synchronizingTabs = true;
        try
        {
            var tab = _views[document.Id];
            _displayedDocument = document; _activeEditor = (RichTextBox)tab.Content;
            var text = new TextRange(_activeEditor.Document.ContentStart, _activeEditor.Document.ContentEnd).Text;
            if (text.EndsWith("\r\n", StringComparison.Ordinal)) text = text[..^2];
            if (text != document.Text)
            {
                _activeEditor.IsUndoEnabled = false;
                new TextRange(_activeEditor.Document.ContentStart, _activeEditor.Document.ContentEnd).Text = document.Text;
                foreach (var paragraph in _activeEditor.Document.Blocks.OfType<Paragraph>()) paragraph.Margin = new Thickness(0);
                _activeEditor.IsUndoEnabled = true;
            }
            DocumentTabs.SelectedItem = tab;
        }
        finally { _synchronizingTabs = false; }
    }

    private void DocumentTabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_ready && !_synchronizingTabs && ReferenceEquals(e.Source, DocumentTabs) && DocumentTabs.SelectedItem is TabItem { Tag: EditorDocumentTab document })
            _controller.ActivateEditorTab(document);
    }

    public void ShowEditor()
    {
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
        TextEditor.Focus();
        UpdateFocus();
    }

    internal void CloseForShutdown() { _shutdown = true; Close(); }

    private void UpdateFocus() => _viewModel.SetLocalEditorActive(IsActive && TextEditor.IsKeyboardFocusWithin);

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_shutdown) return;
        e.Cancel = true;
        _controller.EditorWindowHidden();
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
