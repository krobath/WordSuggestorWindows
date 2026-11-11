using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using WordSuggestorWindows.App.Services;

internal static class GuiSmoke
{
    private static int _checks;
    private static readonly WindowsNativeTextAdapter Adapter = new();

    public static async Task Run(string repo)
    {
        var data = Path.Combine(repo, ".artifacts", "tests", "gui-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(data);
        Directory.CreateDirectory(Path.Combine(data, "settings"));
        File.WriteAllText(Path.Combine(data, "settings", "settings-v1.json"), "{\"schemaVersion\":2,\"interfaceLocalePreference\":\"da-DK\"}");
        var path = Path.Combine(data, "wordsuggestor-smoke-" + Guid.NewGuid().ToString("N") + ".txt");
        File.WriteAllText(path, "Jeg vil gerne skri");
        var appExe = Environment.GetEnvironmentVariable("WORDSUGGESTOR_TEST_APP_PATH") ?? Path.Combine(repo, "src", "WordSuggestorWindows.App", "bin", "Debug", "net10.0-windows", "WordSuggestorWindows.App.exe");
        var info = new ProcessStartInfo(appExe) { UseShellExecute = false };
        info.Environment["WORDSUGGESTOR_DATA_ROOT"] = data;
        info.Environment["WORDSUGGESTOR_WINDOWS_STARTUP_TEXT"] = "Jeg vil gerne skri";
        if (Environment.GetEnvironmentVariable("WORDSUGGESTOR_TEST_APP_PATH") is not null)
        {
            info.Environment.Remove("WORDSUGGESTOR_SUGGEST_CLI_PATH");
            info.Environment.Remove("DOTNET_ROOT");
            var systemRoot = Environment.GetEnvironmentVariable("SystemRoot")!;
            info.Environment["PATH"] = Path.Combine(systemRoot, "System32") + ";" + systemRoot + ";" + Path.Combine(systemRoot, "System32", "WindowsPowerShell", "v1.0");
        }
        using var app = Process.Start(info)!;
        IntPtr notepadHandle = IntPtr.Zero;
        var originalForeground = GetForegroundWindow();
        GetCursorPos(out var originalCursor);
        try
        {
            var toolbar = await FindWindow(app.Id, "WordSuggestor Windows");
            Check("toolbar-only startup", !Windows(app.Id).Any(w => w.Current.Name.Contains("Editor")));
            using var launcher = Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true, ArgumentList = { path } });
            var notepad = await WaitValue(() => DesktopWindows().FirstOrDefault(w =>
                w.Current.Name.Contains(Path.GetFileNameWithoutExtension(path), StringComparison.OrdinalIgnoreCase)));
            notepadHandle = new IntPtr(notepad.Current.NativeWindowHandle);
            SetForegroundWindow(notepadHandle);
            var edit = notepad.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit))
                .Cast<AutomationElement>().FirstOrDefault(e => !e.Current.IsOffscreen);
            edit?.SetFocus();
            await Task.Delay(400);
            var snapshot = Adapter.ReadFocused();
            Check("Notepad native adapter available", snapshot is not null);
            Console.WriteLine($"Notepad version: {FileVersionInfo.GetVersionInfo(Process.GetProcessById((int)snapshot!.ProcessId).MainModule!.FileName!).FileVersion}");
            SetSelection(snapshot.Control, snapshot.Text.Length, snapshot.Text.Length);
            await Task.Delay(400);
            var toggle = toolbar.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
            // Explicitly identify the toggle, not an unrelated toolbar action.
            var globalToggle = toolbar.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Aktiver eller deaktiver globale forslag"));
            Check("toolbar action accessible", globalToggle is not null);
            Click(globalToggle!);
            await Task.Delay(200);
            Check("toolbar click preserves Notepad foreground", GetForegroundWindow() == notepadHandle);
            Click(globalToggle!);
            await Task.Delay(300);
            var overlay = await FindWindow(app.Id, "WordSuggestor Suggestions");
            async Task SetCaretPermission(bool enabled)
            {
                Click(toolbar.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Indstillinger")));
                var preferences = await FindWindow(app.Id, "WordSuggestor Indstillinger");
                var general = preferences.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "SettingsCategory_general"));
                ((SelectionItemPattern)general.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
                var permission = await WaitValue(() => preferences.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "SettingsControl_G03")));
                var togglePermission = (TogglePattern)permission.GetCurrentPattern(TogglePattern.Pattern);
                if ((togglePermission.Current.ToggleState == ToggleState.On) != enabled) Click(permission);
                await Task.Delay(200);
                var close = preferences.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "SettingsClose"));
                ((InvokePattern)close.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
                SetForegroundWindow(notepadHandle); await Task.Delay(500);
                _ = await Candidate(overlay);
            }
            AutomationElement? FollowChoice() => overlay.FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "FollowCaretPlacement"));
            await SetCaretPermission(false);
            Check("disabled caret permission removes overlay choice", FollowChoice() is not { Current.IsOffscreen: false });
            var staticOrigin = overlay.Current.BoundingRectangle.TopLeft;
            SetSelection(snapshot.Control, snapshot.Text.Length - 1, snapshot.Text.Length - 1); await Task.Delay(500);
            _ = await Candidate(overlay);
            Check("disabled follow keeps external overlay static across caret moves", (overlay.Current.BoundingRectangle.TopLeft - staticOrigin).Length < 1);
            SetSelection(snapshot.Control, snapshot.Text.Length, snapshot.Text.Length); await Task.Delay(500);
            await SetCaretPermission(true);
            var followChoice = FollowChoice();
            Check("overlay has usable native width", overlay.Current.BoundingRectangle.Width >= 292);
            Check("enabling permission exposes choice without selecting follow", followChoice is { Current.IsOffscreen: false } && ((TogglePattern)followChoice.GetCurrentPattern(TogglePattern.Pattern)).Current.ToggleState == ToggleState.Off);
            Click(followChoice!); await Task.Delay(500);
            var placementSnapshot = new WindowsAppSettingsService(Path.Combine(data, "settings", "settings-v1.json")).Load();
            Console.WriteLine($"DIAGNOSTIC follow foregroundMatches={GetForegroundWindow() == notepadHandle} toggle={((TogglePattern)followChoice!.GetCurrentPattern(TogglePattern.Pattern)).Current.ToggleState} savedMode={placementSnapshot.SuggestionPlacementMode} permission={placementSnapshot.FollowCaretOptionEnabled}");
            Check("explicit overlay follow choice preserves Notepad focus", GetForegroundWindow() == notepadHandle && ((TogglePattern)followChoice!.GetCurrentPattern(TogglePattern.Pattern)).Current.ToggleState == ToggleState.On);
            Check("permission and placement changes never alter target text", ReadControlText(snapshot.Control) == snapshot.Text);
            var candidate = await Candidate(overlay);
            var term = candidate.Current.Name;
            var clipboardSequence = GetClipboardSequenceNumber();
            Click(candidate);
            await Task.Delay(500);
            snapshot = Adapter.ReadFocused();
            Check("Notepad mouse accept and caret", snapshot is not null && snapshot.Text == "Jeg vil gerne " + term + " " && snapshot.Start == snapshot.Text.Length && snapshot.End == snapshot.Start);
            Check("external commit preserves clipboard", GetClipboardSequenceNumber() == clipboardSequence);
            var oldSnapshot = snapshot!;
            SetText(snapshot!.Control, "Jeg vil gerne skri");
            SetSelection(snapshot.Control, 18, 18);
            await Task.Delay(500);
            candidate = await Candidate(overlay);
            term = candidate.Current.Name;
            Key(0x11, false); Key(0x31, false); Key(0x31, true); Key(0x11, true);
            await Task.Delay(500);
            snapshot = Adapter.ReadFocused();
            Check("Notepad Ctrl+1 same commit result", snapshot is not null && snapshot.Text == "Jeg vil gerne " + term + " ");
            Check("stale external snapshot rejected", !Adapter.TryCommit(oldSnapshot, "anything", "WRONG", out _));

            // Actual mouse click while a real external app is foreground.
            var open = toolbar.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "OpenEditor"));
            Click(open);
            var editor = await FindWindow(app.Id, "WordSuggestor – Editor");
            var editorHandle = new IntPtr(editor.Current.NativeWindowHandle);
            Check("normal editor becomes foreground over Notepad", GetForegroundWindow() == editorHandle);
            var document = editor.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "InternalEditor"));
            Check("editor accessibility name", document is not null && document.Current.Name == "Dokumenttekst");
            var docPattern = (TextPattern)document!.GetCurrentPattern(TextPattern.Pattern);
            Check("editor text available to accessibility", docPattern.DocumentRange.GetText(-1).StartsWith("Jeg vil gerne skri", StringComparison.Ordinal));
            candidate = await Candidate(overlay);
            term = candidate.Current.Name;
            Click(candidate);
            await Task.Delay(350);
            Check("local mouse accept preserves editor focus", GetForegroundWindow() == editorHandle);
            Check("local mouse accept updates actual document", docPattern.DocumentRange.GetText(-1).TrimEnd('\r', '\n') == "Jeg vil gerne " + term + " ");
            Check("local caret after inserted word", docPattern.GetSelection().Single().GetText(-1) == "");
            // Multi-paragraph Unicode text through the actual input stack.
            document.SetFocus();
            Key(0x11, false); Key(0x41, false); Key(0x41, true); Key(0x11, true);
            Type("Æøå 😀\r\nJeg vil gerne skri");
            await Task.Delay(550);
            candidate = await Candidate(overlay);
            term = candidate.Current.Name;
            Key(0x11, false); Key(0x31, false); Key(0x31, true); Key(0x11, true);
            await Task.Delay(400);
            var actual = docPattern.DocumentRange.GetText(-1).TrimEnd('\r', '\n');
            Check("actual multiline Unicode keyboard accept", actual == "Æøå 😀\r\nJeg vil gerne " + term + " ");
            document.SetFocus();
            Key(0x11, false); Key(0x41, false); Key(0x41, true); Key(0x11, true);
            Type("en gammel bog");
            await Task.Delay(200);
            var selection = docPattern.DocumentRange.Clone();
            selection.MoveEndpointByRange(TextPatternRangeEndpoint.End, selection, TextPatternRangeEndpoint.Start);
            selection.MoveEndpointByUnit(TextPatternRangeEndpoint.End, TextUnit.Character, 9);
            selection.MoveEndpointByUnit(TextPatternRangeEndpoint.Start, TextUnit.Character, 3);
            selection.Select();
            await Task.Delay(300);
            Check("actual editor selection preserved", docPattern.GetSelection().Single().GetText(-1) == "gammel");
            candidate = await Candidate(overlay);
            term = candidate.Current.Name;
            Click(candidate);
            await Task.Delay(300);
            var selectedResult = "en " + term + " bog";
            Check("actual selection candidate replacement", docPattern.DocumentRange.GetText(-1).TrimEnd('\r', '\n') == selectedResult);
            document.SetFocus();
            Key(0x11, false); Key(0x53, false); Key(0x53, true); Key(0x11, true);
            await Task.Delay(350);
            var saveDialog = await FindWindow(app.Id, "Gem WordSuggestor-tekst");
            var filename = saveDialog.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "1001"));
            Check("native Save dialog keyboard access", filename is not null);
            var savedPath = Path.Combine(data, "saved-editor.txt");
            // Shell file dialogs may expose SetValue without committing their
            // filename model. Use the actual keyboard path and a unique file.
            filename!.SetFocus();
            Key(0x11, false); Key(0x41, false); Key(0x41, true); Key(0x11, true);
            Type(savedPath);
            Key(0x0D, false); Key(0x0D, true);
            await WaitValue(() => File.Exists(savedPath) ? savedPath : null);
            Check("native document save preserves text", File.ReadAllText(savedPath) == selectedResult);
            var openPath = Path.Combine(data, "open-editor.txt");
            File.WriteAllText(openPath, "Åbnet tekst med æøå og 😀");
            document.SetFocus();
            Key(0x11, false); Key(0x4F, false); Key(0x4F, true); Key(0x11, true);
            var openDialog = await FindWindow(app.Id, "Åbn WordSuggestor-tekst");
            var openName = openDialog.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.AutomationIdProperty, "1148"),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)))
                ?? openDialog.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "1001"));
            Check("native Open dialog keyboard access", openName is not null);
            openName!.SetFocus();
            Key(0x11, false); Key(0x41, false); Key(0x41, true); Key(0x11, true);
            Type(openPath);
            Key(0x0D, false); Key(0x0D, true);
            await WaitValue(() => docPattern.DocumentRange.GetText(-1).TrimEnd('\r', '\n') == "Åbnet tekst med æøå og 😀" ? "opened" : null);
            Check("native document open preserves Unicode", true);
            Check("local commit leaves Notepad unchanged", ReadControlText(snapshot!.Control) == snapshot.Text);
            // Losing focus must invalidate a pending commit rather than type elsewhere.
            var guarded = Adapter.ReadFocused();
            SetForegroundWindow(notepadHandle);
            await Task.Delay(250);
            Check("editor and external target isolated", GetForegroundWindow() == notepadHandle);
            ((WindowPattern)editor.GetCurrentPattern(WindowPattern.Pattern)).Close();
            await Task.Delay(200);
            Check("closing editor keeps toolbar", toolbar.Current.IsEnabled && !app.HasExited);
            using var duplicate = Process.Start(info)!;
            Check("second launch exits without duplicate toolbar", duplicate.WaitForExit(5000) && duplicate.ExitCode == 0 && !app.HasExited);
            Console.WriteLine($"PASS GUI {_checks} checks. DPI={GetDpiForWindow(notepadHandle)}; monitors={GetSystemMetrics(80)}. Data: {data}");
        }
        finally
        {
            // Only close windows created by this run; never other user documents.
            if (notepadHandle != IntPtr.Zero)
            {
                // Save the synthetic smoke document so Notepad closes without a save prompt.
                SetForegroundWindow(notepadHandle);
                Key(0x11, false); Key(0x53, false); Key(0x53, true); Key(0x11, true);
                await Task.Delay(150);
                PostMessageW(notepadHandle, 0x0010, IntPtr.Zero, IntPtr.Zero);
            }
            if (!app.HasExited)
            {
                var toolbar = Windows(app.Id).FirstOrDefault(w => w.Current.Name == "WordSuggestor Windows");
                if (toolbar is not null) PostMessageW(new IntPtr(toolbar.Current.NativeWindowHandle), 0x0010, IntPtr.Zero, IntPtr.Zero);
                await Task.Delay(300);
                if (!app.HasExited) app.Kill(true);
            }
            SetForegroundWindow(originalForeground);
            SetCursorPos(originalCursor.X, originalCursor.Y);
        }
    }

    private static AutomationElement[] DesktopWindows() => AutomationElement.RootElement.FindAll(TreeScope.Children, System.Windows.Automation.Condition.TrueCondition).Cast<AutomationElement>().ToArray();
    private static AutomationElement[] Windows(int pid) => AutomationElement.RootElement.FindAll(TreeScope.Children,
        new PropertyCondition(AutomationElement.ProcessIdProperty, pid)).Cast<AutomationElement>().ToArray();
    private static Task<AutomationElement> FindWindow(int pid, string title) => WaitValue(() =>
    {
        foreach (var window in Windows(pid))
        {
            if (window.Current.Name == title) return window;
            // Owned native dialogs are UIA descendants of the WPF editor, not
            // necessarily direct children of the desktop automation root.
            var owned = window.FindFirst(TreeScope.Descendants, new AndCondition(
                new PropertyCondition(AutomationElement.NameProperty, title),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)));
            if (owned is not null) return owned;
        }
        return null;
    });
    private static Task<AutomationElement> Candidate(AutomationElement overlay) => WaitValue(() => overlay.FindFirst(TreeScope.Descendants,
        new PropertyCondition(AutomationElement.AutomationIdProperty, "AcceptCandidate")));
    private static async Task<T> WaitValue<T>(Func<T?> read) where T : class
    {
        var until = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < until) { var value = read(); if (value is not null) return value; await Task.Delay(100); }
        throw new TimeoutException("GUI element unavailable");
    }
    private static void Check(string label, bool value)
    { if (!value) throw new Exception("GUI: " + label); _checks++; Console.WriteLine("PASS GUI " + label); }
    private static void Click(AutomationElement element)
    {
        var rect = element.Current.BoundingRectangle;
        if (rect.IsEmpty || element.Current.IsOffscreen) throw new Exception("GUI target not visible");
        SetCursorPos((int)(rect.Left + rect.Width / 2), (int)(rect.Top + rect.Height / 2));
        mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero); mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
    }
    private static void Key(byte key, bool up) => keybd_event(key, 0, up ? 2u : 0u, UIntPtr.Zero);
    private static void Type(string text)
    {
        foreach (var character in text)
        {
            if (character == '\r') continue;
            if (character == '\n') { Key(0x0D, false); Key(0x0D, true); continue; }
            var inputs = new[] { new INPUT { type = 1, scan = character, flags = 4 }, new INPUT { type = 1, scan = character, flags = 6 } };
            if (SendInput(2, inputs, Marshal.SizeOf<INPUT>()) != 2) throw new Exception("GUI input failed");
        }
    }
    private static void SetText(IntPtr control, string text) => SendMessageText(control, 0x000C, IntPtr.Zero, text);
    private static void SetSelection(IntPtr control, int start, int end) => SendMessageW(control, 0x00B1, new IntPtr(start), new IntPtr(end));
    private static string ReadControlText(IntPtr control)
    {
        var buffer = new System.Text.StringBuilder(60001);
        GetWindowTextW(control, buffer, buffer.Capacity);
        // WM_GETTEXT is required for another process's child controls.
        SendMessageBuffer(control, 0x000D, new IntPtr(buffer.Capacity), buffer);
        return buffer.ToString();
    }
    [StructLayout(LayoutKind.Explicit, Size = 40)] private struct INPUT
    { [FieldOffset(0)] public uint type; [FieldOffset(8)] public ushort vk; [FieldOffset(10)] public ushort scan; [FieldOffset(12)] public uint flags; }
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X, Y; }
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, INPUT[] input, int size);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT point);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")] private static extern bool PostMessageW(IntPtr handle, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern IntPtr SendMessageW(IntPtr handle, uint msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessageText(IntPtr handle, uint msg, IntPtr wParam, string text);
    [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessageBuffer(IntPtr handle, uint msg, IntPtr wParam, System.Text.StringBuilder buffer);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextW(IntPtr handle, System.Text.StringBuilder buffer, int size);
    [DllImport("user32.dll")] private static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr handle);
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
}
