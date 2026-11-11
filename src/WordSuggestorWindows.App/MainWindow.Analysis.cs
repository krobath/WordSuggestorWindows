using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WordSuggestorWindows.App.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class MainWindow
{
    private AnalysisRangeAdorner? _analysisAdorner;
    private ContextMenu? _analysisMenu;
    private bool _analysisPresentationShutdown;
    private string _analysisCommitRoute = "idle";
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<RichTextBox, object> _analysisEditors = new();
    private static readonly Dictionary<string, Color> ClassColors = new()
    {
        ["noun"] = Colors.MediumPurple, ["properNoun"] = Colors.HotPink, ["verb"] = Colors.Teal,
        ["adjective"] = Colors.DarkOrange, ["adverb"] = Colors.SlateBlue, ["pronoun"] = Colors.ForestGreen,
        ["determiner"] = Colors.Sienna, ["preposition"] = Colors.DodgerBlue, ["conjunction"] = Colors.Turquoise, ["numeral"] = Colors.DarkOliveGreen
    };
    private void DisposeAnalysisPresentation()
    {
        if (_analysisMenu is { } menu) { _analysisMenu = null; menu.IsOpen = false; }
        if (_analysisAdorner is { } old && AdornerLayer.GetAdornerLayer(old.AdornedElement) is { } layer) layer.Remove(old);
        _analysisAdorner = null;
    }
    private void ShutdownAnalysisPresentation()
    { _analysisPresentationShutdown = true; _viewModel.AnalysisCommitRequested = null; DisposeAnalysisPresentation(); }
    private void RenderDocumentAnalysis()
    {
        DisposeAnalysisPresentation();
        var editor = EditorTextBox;
        _analysisEditors.GetValue(editor, box =>
        {
            box.AddHandler(FrameworkElement.ContextMenuOpeningEvent, new ContextMenuEventHandler(AnalysisContextMenuOpening), true);
            box.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((_, _) => _analysisAdorner?.InvalidateVisual()));
            return new object();
        });
        if (_viewModel.AnalysisDocument is not { } snapshot || snapshot.Text != GetEditorPlainText() || AdornerLayer.GetAdornerLayer(editor) is not { } layer) return;
        var spans = new List<AnalysisPaintSpan>();
        if (_viewModel.LoadSettingsSnapshot().IsTextAnalyzerColoringEnabled)
            foreach (var word in snapshot.Result.Readings.Words)
            {
                if (!ClassColors.TryGetValue(_viewModel.DisplayWordClass(word), out var color)) continue;
                color.A = 45;
                spans.Add(new(GetTextPointerAtCharOffset(word.Start), GetTextPointerAtCharOffset(word.Start + word.Length), new SolidColorBrush(color), false));
            }
        foreach (var diagnostic in snapshot.Result.Diagnostics)
            spans.Add(new(GetTextPointerAtCharOffset(diagnostic.Start), GetTextPointerAtCharOffset(diagnostic.Start + Math.Max(0, diagnostic.Length)),
                diagnostic.IssueType == "punctuation" ? Brushes.RoyalBlue : Brushes.Firebrick, true));
        _analysisAdorner = new(editor) { Spans = spans, IsHitTestVisible = false, ClipToBounds = true };
        layer.Add(_analysisAdorner);
    }
    private bool CommitAnalysisEdits(AnalysisEditTransaction transaction)
    {
        if (!_editorWindow.IsActive || !EditorTextBox.IsKeyboardFocusWithin || GetEditorPlainText() != transaction.Original) return false;
        EditorTextBox.BeginChange();
        try
        {
            foreach (var edit in transaction.Edits)
                new TextRange(GetTextPointerAtCharOffset(edit.Start), GetTextPointerAtCharOffset(edit.Start + edit.Length)).Text = edit.Replacement;
            SetEditorCaretIndex(transaction.Caret);
        }
        finally { EditorTextBox.EndChange(); }
        return GetEditorPlainText() == transaction.Result;
    }
    private bool RestoreAnalysisMenuTarget(ContextMenu menu, RichTextBox editor, AnalysisDocumentSnapshot snapshot)
    {
        var source = PresentationSource.FromVisual(menu) as System.Windows.Interop.HwndSource;
        var foreground = _selectionImportService.CurrentForegroundWindow;
        var editorHandle = new System.Windows.Interop.WindowInteropHelper(_editorWindow).Handle;
        if (source is null && foreground == editorHandle && _editorWindow.IsActive && ReferenceEquals(editor, EditorTextBox) &&
            ReferenceEquals(snapshot, _viewModel.AnalysisDocument) && GetEditorPlainText() == snapshot.Text)
        { editor.Focus(); _analysisCommitRoute = "verified_active_editor"; return editor.IsKeyboardFocusWithin; }
        if (source is null) { _analysisCommitRoute = "menu_closed"; return false; }
        if (foreground != source.Handle && foreground != editorHandle) { _analysisCommitRoute = "foreground_changed"; return false; }
        if (!ReferenceEquals(_analysisMenu, menu) || !ReferenceEquals(editor, EditorTextBox) || !ReferenceEquals(snapshot, _viewModel.AnalysisDocument) || GetEditorPlainText() != snapshot.Text)
        { _analysisCommitRoute = "document_changed"; return false; }
        // An explicit click in this verified owned menu authorizes normal focus
        // return to its originating editor. Never activate from another app.
        menu.IsOpen = false; _editorWindow.Activate(); editor.Focus();
        var ready = _editorWindow.IsActive && editor.IsKeyboardFocusWithin && _selectionImportService.CurrentForegroundWindow == editorHandle;
        _analysisCommitRoute = ready ? "verified_editor" : "focus_return_rejected";
        return ready;
    }
    private void WireAnalysisAction(MenuItem item, Action action)
    {
        var delivered = false;
        void Commit() { if (delivered) return; delivered = true; action(); }
        // Handle the explicit gesture before WPF tears down the popup source.
        // UI Automation's Click route uses the same once-only action.
        item.PreviewMouseLeftButtonUp += (_, e) => { e.Handled = true; Commit(); };
        item.PreviewKeyDown += (_, e) => { if (e.Key is not (Key.Enter or Key.Space)) return; e.Handled = true; Commit(); };
        item.Click += (_, _) => Commit();
    }
    private void AnalysisContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (!ReferenceEquals(sender, EditorTextBox) || _viewModel.AnalysisDocument is not { } snapshot || snapshot.Text != GetEditorPlainText()) return;
        var editor = EditorTextBox;
        var pointer = e.CursorLeft < 0 ? editor.CaretPosition : editor.GetPositionFromPoint(Mouse.GetPosition(editor), true);
        if (pointer is null) return;
        var offset = GetTextOffset(pointer);
        var word = snapshot.Result.Readings.Words.FirstOrDefault(w => offset >= w.Start && offset <= w.Start + w.Length);
        var findings = snapshot.Result.Diagnostics.Where(d => offset >= d.Start && offset <= d.Start + Math.Max(1, d.Length)).ToArray();
        if (word is null && findings.Length == 0) return; // retain native editing menu
        var menu = new ContextMenu { PlacementTarget = editor, Placement = PlacementMode.MousePoint };
        _analysisMenu = menu;
        menu.Closed += (_, _) => { if (ReferenceEquals(editor.ContextMenu, menu)) editor.ClearValue(FrameworkElement.ContextMenuProperty); if (ReferenceEquals(_analysisMenu, menu)) _analysisMenu = null; if (!_analysisPresentationShutdown && !Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(() => SyncOverlayVisibility()); };
        foreach (var diagnostic in findings)
        {
            menu.Items.Add(new MenuItem { Header = new TextBlock { Text = diagnostic.Message, TextWrapping = TextWrapping.Wrap, MaxWidth = 360 }, IsEnabled = false });
            var alternatives = diagnostic.Edits.Length > 0 ? 1 : diagnostic.Replacements.Length;
            for (var index = 0; index < alternatives; index++)
            {
                var alternative = index;
                var label = diagnostic.Edits.Length > 0 ? string.Join(" · ", diagnostic.Edits.Select(x => x.Replacement)) : diagnostic.Replacements[index];
                var item = new MenuItem { Header = SettingsText.Get("ApplyCorrection") + " “" + label + "”" };
                WireAnalysisAction(item, () =>
                {
                    if (!RestoreAnalysisMenuTarget(menu, editor, snapshot)) return;
                    if (diagnostic.Requirement == "meaning_dependent" && MessageBox.Show(_editorWindow, SettingsText.Get("CorrectionConfirm"), "WordSuggestor", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                    _analysisCommitRoute = _viewModel.TryApplyDiagnostic(diagnostic, alternative) ? "committed" : "commit_rejected";
                });
                menu.Items.Add(item);
            }
        }
        if (word is not null)
        {
            foreach (var reading in word.Readings)
            {
                var item = new MenuItem { Header = SettingsText.WordClassLabel(reading.WordClass) + " · " + reading.Gram, IsEnabled = word.Ambiguous };
                System.Windows.Automation.AutomationProperties.SetAutomationId(item, "ChooseCoreReading");
                WireAnalysisAction(item, () => { if (RestoreAnalysisMenuTarget(menu, editor, snapshot)) _viewModel.TryChooseReading(word, reading); });
                menu.Items.Add(item);
            }
        }
        menu.Items.Add(new Separator());
        foreach (var (key, command) in new[] { ("EditCut", ApplicationCommands.Cut), ("EditCopy", ApplicationCommands.Copy), ("EditPaste", ApplicationCommands.Paste), ("EditSelectAll", ApplicationCommands.SelectAll) })
            menu.Items.Add(new MenuItem { Header = SettingsText.Get(key), Command = command, CommandTarget = editor });
        editor.ContextMenu = menu; e.Handled = true; menu.IsOpen = true;
        SyncOverlayVisibility();
    }
}
