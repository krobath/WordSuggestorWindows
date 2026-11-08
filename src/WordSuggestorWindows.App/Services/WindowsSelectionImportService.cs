using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsSelectionImportService
{
    private const int ClipboardCopyDelayMs = 160;
    private const int ForegroundActivationDelayMs = 140;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const ushort VirtualKeyControl = 0x11;
    private const ushort VirtualKeyC = 0x43;
    private const string WordProcessName = "WINWORD";

    public event EventHandler<SelectionImportDiagnostic>? DiagnosticEmitted;

    public IntPtr CurrentForegroundWindow => GetForegroundWindow();

    public SelectionImportResult? TryReadSelectionFromForegroundWindow(
        IntPtr excludedWindowHandle,
        bool emitDiagnostics = false)
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            EmitIf(emitDiagnostics, "UIA", "Skipped", "No foreground window is currently available.");
            return null;
        }

        if (foreground == excludedWindowHandle || WindowsNativeTextAdapter.IsOwnWindow(foreground))
        {
            EmitIf(emitDiagnostics, "UIA", "Skipped", "Foreground window is WordSuggestor.");
            return null;
        }

        var windowSelection = TryReadSelectionFromWindow(foreground);
        if (windowSelection is not null)
        {
            EmitIf(
                emitDiagnostics,
                "UIA",
                "Success",
                $"Foreground window exposed {windowSelection.Text.Length} characters. {DescribeWindowHandle(foreground)}");
            return windowSelection;
        }

        EmitIf(
            emitDiagnostics,
            "UIA",
            "NoSelection",
            $"Foreground window did not expose selected text through TextPattern. {DescribeWindowHandle(foreground)}");
        return null;
    }

    public ExternalSuggestionAnchorSnapshot? TryReadSuggestionAnchorFromForegroundWindow(
        IntPtr excludedWindowHandle,
        bool emitDiagnostics = false)
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero || foreground == excludedWindowHandle)
        {
            return null;
        }

        return TryReadSuggestionAnchorFromWindowHandle(foreground, excludedWindowHandle, "foreground", emitDiagnostics);
    }

    public ExternalSuggestionAnchorSnapshot? TryReadSuggestionAnchorFromWindowHandle(
        IntPtr windowHandle,
        IntPtr excludedWindowHandle,
        string source,
        bool emitDiagnostics = false)
    {
        if (windowHandle == IntPtr.Zero || windowHandle == excludedWindowHandle)
        {
            return null;
        }

        try
        {
            var root = AutomationElement.FromHandle(windowHandle);
            if (root is null)
            {
                return null;
            }

            var focused = root.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.HasKeyboardFocusProperty, true));
            if (focused?.Current.IsPassword == true) return null;
            var anchor = focused is not null
                ? TryReadTextPatternAnchor(focused, "Windows UI Automation focused descendant", windowHandle)
                : TryReadTextPatternAnchor(root, "Windows UI Automation window", windowHandle);

            if (anchor is not null)
            {
                EmitIf(
                    emitDiagnostics,
                    "SuggestionAnchor",
                    "Success",
                    $"{source} anchor resolved via {anchor.Source} quality={anchor.Quality} rect={anchor.ScreenRect} {DescribeWindowHandle(windowHandle)}");
            }
            else
            {
                EmitIf(
                    emitDiagnostics,
                    "SuggestionAnchor",
                    "NoAnchor",
                    $"{source} window did not expose a suggestion anchor. {DescribeWindowHandle(windowHandle)}");
            }

            return anchor;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public SelectionImportResult? TryReadSelectionFromWindowHandle(
        IntPtr windowHandle,
        IntPtr excludedWindowHandle,
        string source,
        bool emitDiagnostics = false)
    {
        if (windowHandle == IntPtr.Zero)
        {
            EmitIf(emitDiagnostics, "UIA", "Skipped", $"{source} window handle was unavailable.");
            return null;
        }

        if (windowHandle == excludedWindowHandle || WindowsNativeTextAdapter.IsOwnWindow(windowHandle))
        {
            EmitIf(emitDiagnostics, "UIA", "Skipped", $"{source} window handle points to WordSuggestor.");
            return null;
        }

        var officeSelection = TryReadOfficeSelectionFromWindowHandle(windowHandle, emitDiagnostics, source);
        if (officeSelection is not null)
        {
            return officeSelection;
        }

        var windowSelection = TryReadSelectionFromWindow(windowHandle);
        if (windowSelection is not null)
        {
            EmitIf(
                emitDiagnostics,
                "UIA",
                "Success",
                $"{source} window exposed {windowSelection.Text.Length} characters. {DescribeWindowHandle(windowHandle)}");
            return windowSelection;
        }

        EmitIf(
            emitDiagnostics,
            "UIA",
            "NoSelection",
            $"{source} window did not expose selected text through TextPattern. {DescribeWindowHandle(windowHandle)}");
        return null;
    }

    public async Task<SelectionImportResult?> TryReadSelectionWithClipboardFallbackAsync(
        IntPtr targetWindowHandle, IntPtr returnWindowHandle)
    {
        var adapter = new WindowsNativeTextAdapter();
        var expected = adapter.ReadFocused();
        if (expected is null || expected.Window != targetWindowHandle || expected.Start == expected.End)
        {
            Emit("ClipboardFallback", "UnqualifiedTarget", "Explicit copy requires a verified Notepad selection.");
            return null;
        }
        // Materialize the clipboard before replacing it. Deferred COM data objects
        // cannot be trusted to remain usable after the clipboard owner changes.
        var original = SnapshotClipboard();
        if (original is null) return null;
        var sentinel = "WordSuggestor.Copy." + Guid.NewGuid().ToString("N");
        if (adapter.ReadFocused() != expected || !TrySetClipboardSentinel(sentinel)) return null;
        var ownedSequence = GetClipboardSequenceNumber();
        try
        {
            if (adapter.ReadFocused() != expected || SendCopyShortcut() != 4) return null;
            await Task.Delay(ClipboardCopyDelayMs);
            var sequence = GetClipboardSequenceNumber();
            GetWindowThreadProcessId(GetClipboardOwner(), out var ownerPid);
            if (sequence != ownedSequence)
            {
                if (ownerPid != expected.ProcessId) return null; // another copy wins
                ownedSequence = sequence;
            }
            if (adapter.ReadFocused() != expected) return null;
            var copied = TryGetClipboardText();
            if (copied != expected.Text[expected.Start..expected.End]) return null;
            Emit("ClipboardFallback", "Success", "Verified explicit copy; no text recorded.");
            return new SelectionImportResult(copied, "Notepad clipboard-import", DateTimeOffset.Now, expected.Window);
        }
        finally
        {
            if (GetClipboardSequenceNumber() == ownedSequence)
                Emit("ClipboardFallback", RestoreClipboard(original) ? "ClipboardRestored" : "ClipboardRestoreFailed", "Explicit copy cleanup.");
        }
    }

    private static IDataObject? SnapshotClipboard()
    {
        try
        {
            var source = Clipboard.GetDataObject();
            var copy = new DataObject();
            if (source is null) return copy;
            var formats = source.GetFormats(false);
            if (formats.Length > 32) return null;
            foreach (var format in formats)
            {
                var value = source.GetData(format, false);
                object? cloned = value switch
                {
                    string text => text,
                    byte[] bytes => bytes.ToArray(),
                    MemoryStream stream => new MemoryStream(stream.ToArray()),
                    System.Windows.Media.Imaging.BitmapSource bitmap => bitmap.Clone(),
                    _ => null
                };
                if (cloned is null) return null;
                copy.SetData(format, cloned);
            }
            return copy;
        }
        catch (ExternalException) { return null; }
    }

    private void Emit(string stage, string outcome, string detail) =>
        DiagnosticEmitted?.Invoke(
            this,
            new SelectionImportDiagnostic(DateTimeOffset.Now, stage, outcome, detail));

    private void EmitIf(bool shouldEmit, string stage, string outcome, string detail)
    {
        if (shouldEmit)
        {
            Emit(stage, outcome, detail);
        }
    }

    private static SelectionImportResult? TryReadSelectionFromWindow(IntPtr windowHandle)
    {
        try
        {
            var root = AutomationElement.FromHandle(windowHandle);
            if (root is null)
            {
                return null;
            }

            var focused = root.FindFirst(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.HasKeyboardFocusProperty, true));
            if (focused?.Current.IsPassword == true) return null;

            return focused is not null
                ? TryReadTextPatternSelection(focused, "Windows UI Automation focused descendant", windowHandle)
                : TryReadTextPatternSelection(root, "Windows UI Automation window", windowHandle);
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private SelectionImportResult? TryReadOfficeSelectionFromWindowHandle(
        IntPtr windowHandle,
        bool emitDiagnostics,
        string source)
    {
        if (!IsWordWindow(windowHandle))
        {
            return null;
        }

        try
        {
            var selection = TryReadWordSelection(windowHandle);
            if (selection is null)
            {
                EmitIf(
                    emitDiagnostics,
                    "OfficeSelection",
                    "NoSelection",
                    $"{source} Word window did not expose a stable COM selection. {DescribeWindowHandle(windowHandle)}");
                return null;
            }

            EmitIf(
                emitDiagnostics,
                "OfficeSelection",
                "Success",
                $"{source} Word window exposed {selection.Text.Length} characters through Word COM selection. {DescribeWindowHandle(windowHandle)}");
            return selection;
        }
        catch (Exception ex) when (ex is COMException or InvalidOperationException or Win32Exception)
        {
            EmitIf(
                emitDiagnostics,
                "OfficeSelection",
                "Failed",
                $"{source} Word selection bridge failed safely: {ex.GetType().Name}: {ex.Message}. {DescribeWindowHandle(windowHandle)}");
            return null;
        }
    }

    private SelectionImportResult? TryReadWordSelection(IntPtr windowHandle)
    {
        var command =
            "$ErrorActionPreference = 'Stop'; " +
            "$word = [Runtime.InteropServices.Marshal]::GetActiveObject('Word.Application'); " +
            "if ($null -eq $word) { exit 11 }; " +
            "$selection = $word.Selection; " +
            "if ($null -eq $selection) { exit 12 }; " +
            "if ([int]$selection.Start -eq [int]$selection.End) { exit 13 }; " +
            "$text = [string]$selection.Text; " +
            "$hwnd = 0; " +
            "try { $hwnd = [int64]$word.ActiveWindow.Hwnd } catch { $hwnd = 0 }; " +
            "[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false); " +
            "[Console]::Out.WriteLine('WS_WORD|' + $hwnd); " +
            "[Console]::Out.Write($text);";

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -EncodedCommand {EncodePowerShellCommand(command)}",
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Word selection bridge could not start.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            if (process.ExitCode is 11 or 12 or 13)
            {
                return null;
            }

            throw new InvalidOperationException(
                $"Word selection bridge exited with code {process.ExitCode}: {TrimForDiagnostic(stderr)}");
        }

        var match = Regex.Match(stdout, @"\AWS_WORD\|(?<hwnd>-?\d+)\r?\n", RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            throw new InvalidOperationException("Word selection bridge returned no window metadata.");
        }

        var outputWindowHandle = long.TryParse(match.Groups["hwnd"].Value, out var rawWindowHandle)
            ? new IntPtr(rawWindowHandle)
            : windowHandle;
        var text = stdout[match.Length..].TrimEnd('\r', '\n');
        return string.IsNullOrWhiteSpace(text)
            ? null
            : new SelectionImportResult(text.Trim(), "Microsoft Word COM selection", DateTimeOffset.Now, outputWindowHandle == IntPtr.Zero ? windowHandle : outputWindowHandle);
    }

    private static SelectionImportResult? TryReadTextPatternSelection(AutomationElement? element, string source, IntPtr windowHandle)
    {
        if (element is null)
        {
            return null;
        }

        try
        {
            if (!element.TryGetCurrentPattern(TextPattern.Pattern, out var patternObject) ||
                patternObject is not TextPattern textPattern)
            {
                return null;
            }

            var ranges = textPattern.GetSelection();
            if (ranges.Length == 0)
            {
                return null;
            }

            var text = string.Join(
                Environment.NewLine,
                ranges
                    .Select(range => range.GetText(-1))
                    .Where(value => !string.IsNullOrWhiteSpace(value)))
                .Trim();

            return string.IsNullOrWhiteSpace(text)
                ? null
                : new SelectionImportResult(text, source, DateTimeOffset.Now, windowHandle);
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static ExternalSuggestionAnchorSnapshot? TryReadTextPatternAnchor(AutomationElement? element, string source, IntPtr windowHandle)
    {
        if (element is null)
        {
            return null;
        }

        try
        {
            if (element.TryGetCurrentPattern(TextPattern.Pattern, out var patternObject) &&
                patternObject is TextPattern textPattern)
            {
                foreach (var range in textPattern.GetSelection())
                {
                    var rangeRect = TryResolveRangeRect(range.GetBoundingRectangles());
                    if (rangeRect is not null)
                    {
                        return new ExternalSuggestionAnchorSnapshot(
                            rangeRect.Value,
                            source,
                            DateTimeOffset.Now,
                            windowHandle,
                            SuggestionAnchorQuality.Confirmed);
                    }
                }
            }

            var elementRect = element.Current.BoundingRectangle;
            if (!elementRect.IsEmpty && elementRect.Width > 1 && elementRect.Height > 1)
            {
                return new ExternalSuggestionAnchorSnapshot(
                    new Rect(elementRect.Left, elementRect.Top, elementRect.Width, elementRect.Height),
                    $"{source} (element fallback)",
                    DateTimeOffset.Now,
                    windowHandle,
                    SuggestionAnchorQuality.Approximate);
            }

            return null;
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static Rect? TryResolveRangeRect(Rect[] rects)
    {
        foreach (var rect in rects)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }

            return new Rect(rect.Left, rect.Top, Math.Max(1, rect.Width), Math.Max(18, rect.Height));
        }

        return null;
    }

    private static string? TryGetClipboardText()
    {
        try
        {
            return Clipboard.ContainsText(TextDataFormat.UnicodeText)
                ? Clipboard.GetText(TextDataFormat.UnicodeText)
                : null;
        }
        catch (COMException)
        {
            return null;
        }
        catch (ExternalException)
        {
            return null;
        }
    }

    private static IDataObject? TryGetClipboardDataObject()
    {
        try
        {
            return Clipboard.GetDataObject();
        }
        catch (COMException)
        {
            return null;
        }
        catch (ExternalException)
        {
            return null;
        }
    }

    private static bool TrySetClipboardSentinel(string sentinel)
    {
        try
        {
            Clipboard.SetText(sentinel, TextDataFormat.UnicodeText);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    private static bool RestoreClipboard(IDataObject? originalClipboard)
    {
        try
        {
            if (originalClipboard is null)
            {
                Clipboard.Clear();
                return true;
            }

            Clipboard.SetDataObject(originalClipboard, copy: true);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    private static uint SendCopyShortcut()
    {
        var inputs = new[]
        {
            KeyboardInput(VirtualKeyControl, keyUp: false),
            KeyboardInput(VirtualKeyC, keyUp: false),
            KeyboardInput(VirtualKeyC, keyUp: true),
            KeyboardInput(VirtualKeyControl, keyUp: true)
        };

        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static string DescribeWindowHandle(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return "hwnd=0x0";
        }

        var builder = new StringBuilder();
        builder.Append($"hwnd=0x{windowHandle.ToInt64():X}");

        try
        {
            _ = GetWindowThreadProcessId(windowHandle, out var processId);
            builder.Append($", pid={processId}");

            if (processId != 0)
            {
                try
                {
                    using var process = Process.GetProcessById((int)processId);
                    builder.Append($", process={process.ProcessName}, responding={SafeResponding(process)}");
                }
                catch (ArgumentException)
                {
                    builder.Append(", process=<exited>");
                }
                catch (InvalidOperationException)
                {
                    builder.Append(", process=<unavailable>");
                }
            }

            var className = GetClassNameSafe(windowHandle);
            if (!string.IsNullOrWhiteSpace(className))
            {
                builder.Append($", class=\"{className}\"");
            }
        }
        catch
        {
            builder.Append(", meta=<unavailable>");
        }

        return builder.ToString();
    }

    private static bool IsWordWindow(IntPtr windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        _ = GetWindowThreadProcessId(windowHandle, out var processId);
        if (processId == 0)
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return string.Equals(process.ProcessName, WordProcessName, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string GetWindowTextSafe(IntPtr windowHandle)
    {
        var buffer = new StringBuilder(512);
        return GetWindowText(windowHandle, buffer, buffer.Capacity) > 0 ? buffer.ToString() : string.Empty;
    }

    private static string GetClassNameSafe(IntPtr windowHandle)
    {
        var buffer = new StringBuilder(256);
        return GetClassName(windowHandle, buffer, buffer.Capacity) > 0 ? buffer.ToString() : string.Empty;
    }

    private static bool SafeResponding(Process process)
    {
        try
        {
            return process.Responding;
        }
        catch
        {
            return false;
        }
    }

    private static string TrimForDiagnostic(string value)
    {
        var singleLine = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
        return singleLine.Length <= 500 ? singleLine : singleLine[..500];
    }

    private static string EncodePowerShellCommand(string command) =>
        Convert.ToBase64String(Encoding.Unicode.GetBytes(command));

    private static INPUT KeyboardInput(ushort virtualKey, bool keyUp) =>
        new()
        {
            type = InputKeyboard,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = virtualKey,
                    dwFlags = keyUp ? KeyEventKeyUp : 0
                }
            }
        };

    [DllImport("user32.dll")] private static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll")] private static extern IntPtr GetClipboardOwner();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
}
