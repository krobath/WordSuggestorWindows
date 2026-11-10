using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

namespace WordSuggestorWindows.App;

public partial class MainWindow
{
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<RichTextBox, object> _assistEditors = new();
    private (RichTextBox Editor, string Language, SmartCommitReceipt Receipt)? _smartReceipt;
    private CancellationTokenSource? _assistCancellation;
    private (RichTextBox Editor, string Expected, string Input)? _directInput;
    private (RichTextBox Editor, string Language, long Created, string BeforeText, int Caret)? _lineBreak;
    private (ExternalTextSnapshot Target, string Language, SmartCommitReceipt Receipt)? _externalSmartReceipt;
    private CancellationTokenSource? _externalAssistCancellation;
    private void CancelSuggestionAssist()
    {
        _assistCancellation?.Cancel(); _assistCancellation?.Dispose(); _assistCancellation = null;
        _smartReceipt = null; _directInput = null;
        _lineBreak = null;
        _externalSmartReceipt = null;
        _externalAssistCancellation?.Cancel(); _externalAssistCancellation?.Dispose(); _externalAssistCancellation = null;
    }
    private void SuggestionEditorDeactivated(object? sender, EventArgs e) => CancelSuggestionAssist();
    private void RememberExternalSmartCommit(SuggestionItem item)
    {
        _externalSmartReceipt = null;
        if (_viewModel.LoadSettingsSnapshot().Suggestions.Spacing != "smart" || item.CommitPlan?.AutoSpace != true) return;
        if (new WindowsNativeTextAdapter().ReadFocused() is { } target && target.Start == target.End)
            _externalSmartReceipt = (target, _viewModel.SelectedLanguageOption.LanguageCode, new(target.Text, target.End, Environment.TickCount64));
    }
    private async void ObserveExternalSuggestionInput(ExternalTextSnapshot? snapshot)
    {
        if (_externalSmartReceipt is not { } pending) return;
        if (snapshot == pending.Target) return;
        _externalSmartReceipt = null;
        if (snapshot is null || snapshot.Window != pending.Target.Window || snapshot.Control != pending.Target.Control || snapshot.ProcessId != pending.Target.ProcessId || snapshot.ProcessStarted != pending.Target.ProcessStarted ||
            snapshot.Start != snapshot.End || _viewModel.SelectedLanguageOption.LanguageCode != pending.Language || !_viewModel.IsGlobalCaptureEnabled || _viewModel.CoreSettings is not { } adapter ||
            pending.Receipt.MatchInput(snapshot.Text, snapshot.End, Environment.TickCount64) is not { } input) return;
        _externalAssistCancellation?.Cancel(); _externalAssistCancellation?.Dispose();
        var cancellation = new CancellationTokenSource(); _externalAssistCancellation = cancellation;
        var scope = _viewModel.SuggestionScope;
        try
        {
            var result = await adapter.GetSmartFollowupAsync(input, scope, cancellation.Token);
            if (cancellation.IsCancellationRequested || !result.IsCurrent(scope, pending.Language) || _viewModel.SelectedLanguageOption.LanguageCode != pending.Language ||
                !_viewModel.IsGlobalCaptureEnabled || _viewModel.LoadSettingsSnapshot().Suggestions.Spacing != "smart" || result.Value.Replacement is not { } replacement) return;
            new WindowsNativeTextAdapter().TryReplaceExact(snapshot, pending.Receipt.Caret - 1, snapshot.End, replacement, out _);
        }
        catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException or System.IO.IOException or NotSupportedException) { }
    }
    private void LocalSuggestionOnCommitted(object? sender, SuggestionItem item)
    {
        CancelSuggestionAssist();
        if (_viewModel.LoadSettingsSnapshot().Suggestions.Spacing == "smart" && item.CommitPlan?.AutoSpace == true)
            _smartReceipt = (EditorTextBox, _viewModel.SelectedLanguageOption.LanguageCode,
                new(_viewModel.EditorText, _viewModel.CaretIndex, Environment.TickCount64));
    }
    private void PreviewSuggestionInput(object sender, TextCompositionEventArgs e)
    {
        if (!ReferenceEquals(sender, EditorTextBox) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) return;
        var text = GetEditorPlainText(); var start = GetTextOffset(EditorTextBox.Selection.Start); var end = GetTextOffset(EditorTextBox.Selection.End);
        _directInput = (EditorTextBox, text[..start] + e.Text + text[end..], e.Text);
    }
    private void RegisterSuggestionKey(KeyEventArgs e)
    {
        if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.None && EditorTextBox.Selection.IsEmpty)
            _lineBreak = (EditorTextBox, _viewModel.SelectedLanguageOption.LanguageCode, Environment.TickCount64, GetEditorPlainText(), GetEditorCaretIndex());
        else if (e.Key is Key.Back or Key.Delete or Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.Escape) CancelSuggestionAssist();
    }
    private async void ObserveSuggestionEditorInput()
    {
        if (_isSynchronizingEditorDocument) return;
        var receipt = _smartReceipt; _smartReceipt = null;
        var direct = _directInput; _directInput = null;
        _assistCancellation?.Cancel(); _assistCancellation?.Dispose();
        var cancellation = new CancellationTokenSource(); _assistCancellation = cancellation;
        var token = cancellation.Token;
        var editor = EditorTextBox; var text = GetEditorPlainText(); var caret = GetEditorCaretIndex();
        var language = _viewModel.SelectedLanguageOption.LanguageCode;
        var preferences = _viewModel.LoadSettingsSnapshot().Suggestions;
        var scope = _viewModel.SuggestionScope;
        bool Current() => !token.IsCancellationRequested && ReferenceEquals(EditorTextBox, editor) && _editorWindow.IsActive && editor.IsKeyboardFocusWithin &&
            _viewModel.SelectedLanguageOption.LanguageCode == language && GetEditorPlainText() == text && GetEditorCaretIndex() == caret && editor.Selection.IsEmpty;
        try
        {
            if (!Current() || _viewModel.CoreSettings is not { } adapter) return;
            var caps = await adapter.GetCapabilitiesAsync(cancellationToken: token);
            if (receipt is { } previous && ReferenceEquals(previous.Editor, editor) && previous.Language == language && preferences.Spacing == "smart" && caps.Supports("smartFollowup") &&
                previous.Receipt.MatchInput(text, caret, Environment.TickCount64) is { } input)
            {
                var result = await adapter.GetSmartFollowupAsync(input, scope, token);
                if (Current() && result.IsCurrent(scope, language) && result.Value.Replacement is { } replacement)
                { ReplaceSuggestionAssistRange(previous.Receipt.Caret - 1, input.Length + 1, replacement); return; }
            }
            if (direct is not { } composition || !ReferenceEquals(composition.Editor, editor) || composition.Expected != text || composition.Input.Length != 1 || !preferences.LivePeriodAssist || !caps.LivePeriodAssist || caret > 16000 ||
                _lineBreak is not { } lineBreak || !ReferenceEquals(lineBreak.Editor, editor) || lineBreak.Language != language || Environment.TickCount64 - lineBreak.Created is < 0 or > 8000 ||
                caret <= lineBreak.Caret || !text.StartsWith(lineBreak.BeforeText[..lineBreak.Caret], StringComparison.Ordinal) || text[caret..] != lineBreak.BeforeText[lineBreak.Caret..]) return;
            _lineBreak = null;
            var period = await adapter.GetLivePeriodAsync(text[..caret], scope, token);
            if (Current() && period.IsCurrent(scope, language) && period.Value.Edit is { } edit) ReplaceSuggestionAssistRange(edit.Start, edit.Length, edit.Replacement);
        }
        catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException or System.IO.IOException or NotSupportedException) { }
    }
    private void ReplaceSuggestionAssistRange(int start, int length, string replacement)
    {
        var caret = GetEditorCaretIndex();
        EditorTextBox.BeginChange();
        try
        {
            new TextRange(GetTextPointerAtCharOffset(start), GetTextPointerAtCharOffset(start + length)).Text = replacement;
            SetEditorCaretIndex(caret + replacement.Length - length);
        }
        finally { EditorTextBox.EndChange(); }
    }
}
