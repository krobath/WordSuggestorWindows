using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using WordSuggestorWindows.App;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

internal static class SpeechUiSmoke
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static async Task Run(string repo)
    {
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/WordSuggestorWindows.App;component/SharedStyles.xaml", UriKind.Relative) });
        var root = Path.Combine(repo, ".artifacts", "tests", "speech-ui-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", root);
        var store = new WindowsAppSettingsService();
        store.Save(new() { IsGlobalCaptureEnabled = false, InterfaceLocalePreference = "da-DK", ReadingStrategy = "word", ReadingHighlightMode = "word", UseSystemSpeechSettings = false });
        using var vm = new MainWindowViewModel(new WordSuggestorCoreCliSuggestionProvider(localDataRoot: root), new WindowsErrorInsightsStore(), store, "");
        var main = new MainWindow(vm);
        var editorWindow = Field<InternalEditorWindow>(main, "_editorWindow");
        RichTextBox Editor() => (RichTextBox)typeof(MainWindow).GetProperty("EditorTextBox", Private)!.GetValue(main)!;
        var speech = Field<WindowsTextToSpeechService>(main, "_textToSpeechService");
        var count = 0;
        void Check(string title, bool ok) { if (!ok) throw new Exception(title); count++; Console.WriteLine("PASS SETTINGS SPEECH UI " + title); }
        var foreground = GetForegroundWindow(); GetCursorPos(out var cursor);
        IntPtr notepad = IntPtr.Zero; Window? other = null; SettingsWindow? settings = null;
        try
        {
            main.Show(); vm.EnsureEditorExpanded(); editorWindow.ShowEditor(); await Pause();
            var editorHandle = new System.Windows.Interop.WindowInteropHelper(editorWindow).Handle;
            Check("normal activating editor owns keyboard", GetForegroundWindow() == editorHandle && Editor().IsKeyboardFocusWithin);
            foreach (var key in new byte[] { 0x48, 0x45, 0x4A }) { Key(editorHandle, key); await Task.Delay(70); }
            Check("incomplete word is silent", !speech.IsSpeaking);
            Key(editorHandle, 0x20);
            await Until(() => speech.IsSpeaking);
            Check("physical word boundary starts actual speech", vm.EditorText == "hej " && vm.IsTextToSpeechSpeaking);
            await Until(() => Field<object?>(main, "_currentSpeechHighlightSpan") is not null, 8000);
            Check("native timing paints a separate highlight", Field<object?>(main, "_speechAdorner") is not null);
            Check("highlight keeps selection/caret/focus intact", Editor().Selection.IsEmpty && vm.EditorText == "hej " && Offset(Editor()) == 4 && GetForegroundWindow() == editorHandle);
            await Until(() => !speech.IsSpeaking);
            await Pause();
            Check("completion removes highlight without changing text", Field<object?>(main, "_speechAdorner") is null && vm.EditorText == "hej " && Offset(Editor()) == 4);

            async Task Seed(string text, string strategy, string highlight = "word")
            {
                var snapshot = store.Load(); snapshot.ReadingStrategy = strategy; snapshot.ReadingHighlightMode = highlight; store.Save(snapshot);
                vm.EditorText = text; vm.CaretIndex = text.Length;
                editorWindow.ShowEditor(); Editor().CaretPosition = Editor().Document.ContentEnd; await Pause();
            }
            await Seed("ordet", "word"); Key(editorHandle, 0x20); await Until(() => speech.IsSpeaking);
            var wordProcess = speech.CurrentProcessId; Key(editorHandle, 0x58); await Task.Delay(100);
            Check("typing next word retains committed-word audio without queueing", speech.IsSpeaking && speech.CurrentProcessId == wordProcess && vm.EditorText == "ordet x");
            Call(main, "CancelDocumentSpeech", false);
            await Seed("En sætning.", "sentence"); Key(editorHandle, 0x20); await Until(() => speech.IsSpeaking);
            Check("physical full stop plus space reads sentence", vm.EditorText == "En sætning. ");
            Call(main, "CancelDocumentSpeech", false);
            await Seed("Et afsnit.", "paragraph"); Key(editorHandle, 0x0D); await Until(() => speech.IsSpeaking);
            Check("physical full stop plus Enter reads paragraph", vm.EditorText.StartsWith("Et afsnit.\r\n", StringComparison.Ordinal));
            Call(main, "CancelDocumentSpeech", false);
            await Seed("Ingen lyd", "none"); Key(editorHandle, 0x20); await Task.Delay(600);
            Check("none remains silent after actual boundary", !speech.IsSpeaking && !vm.IsTextToSpeechSpeaking);

            const string longText = "Dette er en længere oplæsning. Markeringen skal følge stemmen og bevare brugerens tekst og markering.";
            await Seed(longText, "none", "sentence");
            var start = Editor().Document.ContentStart.GetInsertionPosition(System.Windows.Documents.LogicalDirection.Forward);
            Editor().Selection.Select(start, start.GetPositionAtOffset(5)!); await Pause();
            var originalSelection = Editor().Selection.Text; var originalCaret = Offset(Editor());
            Call(main, "SpeakText", longText, "syntetisk test", 0); await Until(() => Field<object?>(main, "_currentSpeechHighlightSpan") is not null, 8000);
            Check("sentence highlight does not replace user selection", Editor().Selection.Text == originalSelection && Offset(Editor()) == originalCaret);
            await (Task)Call(main, "SpeakSelectionOrEditorTextAsync")!; await Pause();
            Check("manual toggle stops and preserves selection", !speech.IsSpeaking && Editor().Selection.Text == originalSelection && Offset(Editor()) == originalCaret);
            Call(main, "SpeakText", longText, "syntetisk test", 0); await Until(() => speech.IsSpeaking);
            Key(editorHandle, 0x27); await Pause(); var movedCaret = Offset(Editor());
            Check("caret navigation stops speech without restoring old selection", !speech.IsSpeaking && Editor().Selection.IsEmpty && Offset(Editor()) == movedCaret);
            Call(main, "SpeakText", longText, "syntetisk test", 0); Key(editorHandle, 0x58); await Pause();
            Check("typing cancels speech before stale highlight can apply", !speech.IsSpeaking && Field<object?>(main, "_speechAdorner") is null && vm.EditorText.Contains('x'));

            Call(main, "SpeakText", longText, "syntetisk test", 0);
            if (typeof(MainWindow).GetField("_workspace", Private) is not null)
            {
                Chord(editorHandle, 0x11, 0x54); await Pause();
                Check("working-tree tab switch cancels sound and old presentation", !speech.IsSpeaking && Field<object?>(main, "_speechAdorner") is null && vm.EditorText == "");
            }
            else { Call(main, "CancelDocumentSpeech", false); Console.WriteLine("NOT RUN SPEECH tab UI: editor-tabs implementation is outside this candidate; document identity is contract-tested."); }

            settings = new SettingsWindow(vm); settings.Show(); settings.Activate(); await Pause();
            ((ListBox)settings.FindName("CategoryNavigation")).SelectedIndex = 2; await Pause();
            var speed = (Slider)settings.FindName("ReadingSpeedSlider");
            Check("native accessible speed range is 50–150 percent in five-percent steps", speed.Minimum == 50 && speed.Maximum == 150 && speed.TickFrequency == 5 && speed.SmallChange == 5 && speed.IsEnabled);
            var system = (CheckBox)settings.FindName("UseSystemSpeechSettingsCheckBox");
            var snapshot2 = store.Load(); snapshot2.UseSystemSpeechSettings = true; store.Save(snapshot2); await Pause();
            Check("legacy system setting cannot disable primary speed/voice", speed.IsEnabled && ((ComboBox)settings.FindName("SystemVoiceComboBox")).IsEnabled);
            speed.Focus(); Key(new System.Windows.Interop.WindowInteropHelper(settings).Handle, 0x24); await Pause();
            Check("primary keyboard speed edit saves real tempo and overrides legacy system mode", SpeechSpeedPolicy.Percentage(store.Load().ReadingSpeedDelta) == 50 && !store.Load().UseSystemSpeechSettings && ((TextBlock)settings.FindName("ReadingSpeedValueText")).Text == "50%");
            var voice = (ComboBox)settings.FindName("SystemVoiceComboBox"); voice.SelectedIndex = 1; await Pause();
            Check("primary voice is saved for writing language", store.Load().SystemVoiceIdOverrideByLanguage.TryGetValue("da-dk", out var id) && id == voice.SelectedValue as string);
            var strategy = (ComboBox)settings.FindName("ReadingStrategyComboBox"); strategy.SelectedValue = "word"; await Pause();
            Check("four real strategies and live persistence", strategy.IsEnabled && strategy.Items.Count == 4 && store.Load().ReadingStrategy == "word");
            settings.Close(); settings = null; await Pause();

            var externalSettings = store.Load(); externalSettings.IsGlobalCaptureEnabled = true; externalSettings.ReadingSpeedDelta = 0; store.Save(externalSettings);
            var file = Path.Combine(root, "speech-" + Guid.NewGuid().ToString("N") + ".txt"); File.WriteAllText(file, "hej");
            using var launcher = Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true, ArgumentList = { file } });
            await Until(() => (notepad = FindWindow(Path.GetFileNameWithoutExtension(file))) != IntPtr.Zero, 10000);
            SetForegroundWindow(notepad); await Pause(); Chord(notepad, 0x11, 0x23); await Task.Delay(400);
            var adapter = new WindowsNativeTextAdapter();
            Check("external read target is verified Notepad", adapter.ReadFocused() is { Start: 3, End: 3 } initial && initial.Window == notepad);
            var internalBefore = vm.EditorText; Key(notepad, 0x20); await Until(() => speech.IsSpeaking);
            Check("Notepad typing starts audio without importing or focusing editor", vm.EditorText == internalBefore && GetForegroundWindow() == notepad && Field<object?>(main, "_speechAdorner") is null);
            var externalProcess = speech.CurrentProcessId; Key(notepad, 0x58); await Task.Delay(200);
            Check("Notepad next-word typing retains current committed audio", speech.IsSpeaking && speech.CurrentProcessId == externalProcess);
            Chord(notepad, 0x11, 0x53); // save this synthetic file before closing our window
            other = new Window { Title = "Synthetic unsupported speech target", Width = 300, Height = 150, Content = new PasswordBox() };
            other.Show(); other.Activate(); await Until(() => !speech.IsSpeaking);
            Check("unsupported focus safely stops external automatic speech", !vm.IsTextToSpeechSpeaking && vm.EditorText == internalBefore);
            other.Close(); other = null;
            // Verify settings and voices survive an independent service reopen.
            var reopened = new WindowsAppSettingsService().Load();
            Check("speech preferences survive service restart", reopened.ReadingStrategy == "word" && reopened.SystemVoiceIdOverrideByLanguage.ContainsKey("da-dk"));
            Console.WriteLine($"PASS SETTINGS SPEECH UI {count} checks; physical Windows input, real editor/Notepad and native playback. Human audible/screen-reader sign-off remains open.");
        }
        finally
        {
            speech.Stop(); settings?.Close(); other?.Close();
            if (notepad != IntPtr.Zero) PostMessage(notepad, 0x10, IntPtr.Zero, IntPtr.Zero);
            // Discard only this synthetic profile's drafts, avoiding close prompts
            // from the separately owned working-tree tab implementation.
            vm.EditorText = "";
            if (typeof(MainWindow).GetField("_workspace", Private)?.GetValue(main) is { } workspace)
                foreach (var tab in (IEnumerable)workspace.GetType().GetProperty("Tabs")!.GetValue(workspace)!) tab.GetType().GetMethod("UpdateText")!.Invoke(tab, [""]);
            main.Close();
            if (foreground != IntPtr.Zero) SetForegroundWindow(foreground);
            SetCursorPos(cursor.X, cursor.Y);
        }
    }
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private)!.GetValue(target)!;
    private static object? Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private)!.Invoke(target, args);
    private static int Offset(RichTextBox editor) => new TextRange(editor.Document.ContentStart, editor.CaretPosition).Text.Length;
    private static Task Pause() => Task.Delay(200);
    private static async Task Until(Func<bool> condition, int milliseconds = 5000)
    { var clock = Stopwatch.StartNew(); while (!condition()) { if (clock.ElapsedMilliseconds > milliseconds) throw new Exception("Timed out waiting for speech/UI condition."); await Task.Delay(20); } }
    private static void Key(IntPtr target, byte key)
    { if (GetForegroundWindow() != target) throw new Exception("Synthetic input target changed."); keybd_event(key, 0, 0, UIntPtr.Zero); keybd_event(key, 0, 2, UIntPtr.Zero); }
    private static void Chord(IntPtr target, byte modifier, byte key)
    { if (GetForegroundWindow() != target) throw new Exception("Synthetic input target changed."); keybd_event(modifier, 0, 0, UIntPtr.Zero); try { Key(target, key); } finally { keybd_event(modifier, 0, 2, UIntPtr.Zero); } }
    private static IntPtr FindWindow(string title)
    { var found = IntPtr.Zero; EnumWindows((handle, _) => { var text = new StringBuilder(512); GetWindowText(handle, text, 512); if (text.ToString().Contains(title)) found = handle; return true; }, IntPtr.Zero); return found; }
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    private delegate bool WindowCallback(IntPtr handle, IntPtr argument);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, IntPtr argument);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern int GetWindowText(IntPtr handle, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr handle, uint message, IntPtr first, IntPtr second);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
