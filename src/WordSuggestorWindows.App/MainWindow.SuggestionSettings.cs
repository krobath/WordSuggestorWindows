using System.Windows.Controls;
using System.Windows.Input;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App;

public partial class MainWindow
{
    private readonly Dictionary<int, (int Lane, int Index)> _laneHotKeys = [];
    private string? _laneHotKeyConfiguration;
    private object? _hoverSpeechIdentity;
    private void OverlayHoverSpeechRequested(object? sender, string text) { OverlaySpeechRequested(sender, text); _hoverSpeechIdentity = _textToSpeechService.CurrentIdentity; }
    private void OverlayHoverSpeechCancelled(object? sender, EventArgs e)
    { var identity = _hoverSpeechIdentity; _hoverSpeechIdentity = null; if (_textToSpeechService.IsCurrent(identity)) CancelDocumentSpeech(false); }
    private string[] LaneSchemes()
    {
        var p = _viewModel.LoadSettingsSnapshot().Suggestions;
        return [p.OrdinaryShortcut, p.PrimaryShortcut, p.SecondaryShortcut];
    }
    private void RegisterSuggestionLaneHotKeys()
    {
        if (_windowHandle == IntPtr.Zero) return;
        var schemes = LaneSchemes();
        var active = new[] { _viewModel.VisibleSuggestions.Count > 0, _viewModel.HasPrimarySuggestions, _viewModel.HasSecondarySuggestions };
        var configuration = string.Join(',', schemes) + string.Join(',', active);
        if (configuration == _laneHotKeyConfiguration) return;
        UnregisterSuggestionLaneHotKeys(); _laneHotKeyConfiguration = configuration;
        for (var lane = 0; lane < 3; lane++)
        {
            if (!active[lane]) continue;
            var registered = new List<int>();
            var modifiers = (uint)Services.SuggestionShortcutPolicy.Modifiers(schemes[lane]); // WPF and USER32 use the same modifier bits.
            var success = true;
            for (var i = 0; i < 10; i++)
            {
                var id = 0x5900 + lane * 16 + i;
                if (!RegisterHotKey(_windowHandle, id, modifiers | 0x4000, i == 9 ? 0x30u : 0x31u + (uint)i)) { success = false; break; }
                registered.Add(id);
            }
            if (!success)
            {
                foreach (var id in registered) UnregisterHotKey(_windowHandle, id);
                _viewModel.SetStatusMessage("En forslagsgenvej er optaget. Brug musen eller vælg en anden genvej.");
                continue;
            }
            for (var i = 0; i < registered.Count; i++) _laneHotKeys[registered[i]] = (lane, i);
        }
        // Paging is a separate optional shortcut; never register Ctrl+Shift+arrows.
        if (RegisterHotKey(_windowHandle, ExternalSuggestionPreviousPageHotKeyId, ModControl | 0x4000, VirtualKeyLeft)) _laneHotKeys[ExternalSuggestionPreviousPageHotKeyId] = (-1, -1);
        if (RegisterHotKey(_windowHandle, ExternalSuggestionNextPageHotKeyId, ModControl | 0x4000, VirtualKeyRight)) _laneHotKeys[ExternalSuggestionNextPageHotKeyId] = (-1, 1);
        _areExternalSuggestionHotKeysRegistered = _laneHotKeys.Count > 0;
    }
    private void UnregisterSuggestionLaneHotKeys()
    {
        foreach (var id in _laneHotKeys.Keys) UnregisterHotKey(_windowHandle, id);
        _laneHotKeys.Clear(); _laneHotKeyConfiguration = null; _areExternalSuggestionHotKeysRegistered = false;
    }
    private bool TryHandleSuggestionLaneHotKey(int id)
    {
        if (!_laneHotKeys.TryGetValue(id, out var key)) return false;
        if (!_viewModel.IsExternalSuggestionSessionActive || !_viewModel.IsSuggestionInteractionEnabled || _overlayWindow?.IsVisible != true) return true;
        if (key.Lane < 0) { if (Keyboard.Modifiers == ModifierKeys.Control) _viewModel.ChangeSuggestionPage(key.Index); return true; }
        if (Keyboard.Modifiers != Services.SuggestionShortcutPolicy.Modifiers(LaneSchemes()[key.Lane])) return true;
        _viewModel.AcceptLaneAtIndex(key.Lane, key.Index); return true;
    }
    private bool TryHandleSuggestionDigit(KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var index = key >= Key.D1 && key <= Key.D9 ? key - Key.D1 : key == Key.D0 ? 9 :
            key >= Key.NumPad1 && key <= Key.NumPad9 ? key - Key.NumPad1 : key == Key.NumPad0 ? 9 : -1;
        var lane = Array.FindIndex(LaneSchemes(), s => Services.SuggestionShortcutPolicy.Modifiers(s) == Keyboard.Modifiers);
        if (index < 0 || lane < 0 || !_viewModel.AcceptLaneAtIndex(lane, index)) return false;
        e.Handled = true; return true;
    }
    private void SuggestionSettingsOnChanged(object? sender, AppSettingsSnapshot settings) { CancelSuggestionAssist(); ApplySuggestionEditorLayout(); UnregisterSuggestionLaneHotKeys(); SyncOverlayVisibility(); }
    private void ApplySuggestionEditorLayout()
    {
        if (_editorWindow is null) return;
        _assistEditors.GetValue(EditorTextBox, editor => { editor.PreviewTextInput += PreviewSuggestionInput; return new object(); });
        var wrap = _viewModel.LoadSettingsSnapshot().Suggestions.EditorWrap;
        EditorTextBox.Document.PageWidth = wrap ? double.NaN : 100000;
        EditorTextBox.HorizontalScrollBarVisibility = wrap ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Auto;
    }
}
