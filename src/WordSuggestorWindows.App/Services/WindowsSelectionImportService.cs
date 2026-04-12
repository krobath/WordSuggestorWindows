using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsSelectionImportService
{
    private const int ClipboardCopyDelayMs = 160;
    private const int ForegroundActivationDelayMs = 70;
    private const uint InputKeyboard = 1;
    private const uint KeyEventKeyUp = 0x0002;
    private const ushort VirtualKeyControl = 0x11;
    private const ushort VirtualKeyC = 0x43;

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

        if (foreground == excludedWindowHandle)
        {
            EmitIf(emitDiagnostics, "UIA", "Skipped", "Foreground window is WordSuggestor.");
            return null;
        }

        var focusedSelection = TryReadFocusedSelection("Windows UI Automation focused element");
        if (focusedSelection is not null)
        {
            EmitIf(emitDiagnostics, "UIA", "Success", $"Focused element exposed {focusedSelection.Text.Length} characters.");
            return focusedSelection;
        }

        var windowSelection = TryReadSelectionFromWindow(foreground);
        if (windowSelection is not null)
        {
            EmitIf(emitDiagnostics, "UIA", "Success", $"Foreground window 0x{foreground.ToInt64():X} exposed {windowSelection.Text.Length} characters.");
            return windowSelection;
        }

        EmitIf(emitDiagnostics, "UIA", "NoSelection", $"Foreground window 0x{foreground.ToInt64():X} did not expose selected text through TextPattern.");
        return null;
    }

    public async Task<SelectionImportResult?> TryReadSelectionWithClipboardFallbackAsync(
        IntPtr targetWindowHandle,
        IntPtr returnWindowHandle)
    {
        if (targetWindowHandle == IntPtr.Zero || targetWindowHandle == returnWindowHandle)
        {
            Emit("ClipboardFallback", "Skipped", "No external target window was available.");
            return null;
        }

        var originalClipboard = TryGetClipboardDataObject();
        var sentinel = $"__WordSuggestorClipboardProbe_{Guid.NewGuid():N}__";

        try
        {
            if (!TrySetClipboardSentinel(sentinel))
            {
                Emit("ClipboardFallback", "Failed", "Could not place clipboard sentinel before Ctrl+C probe.");
                return null;
            }

            var foregroundChanged = SetForegroundWindow(targetWindowHandle);
            Emit(
                "ClipboardFallback",
                foregroundChanged ? "TargetActivated" : "TargetActivationUnconfirmed",
                $"Target window 0x{targetWindowHandle.ToInt64():X} was prepared for Ctrl+C.");
            await Task.Delay(ForegroundActivationDelayMs);
            var sentInputCount = SendCopyShortcut();
            Emit("ClipboardFallback", "CopyShortcutSent", $"SendInput reported {sentInputCount} keyboard input events.");
            await Task.Delay(ClipboardCopyDelayMs);

            var copiedText = TryGetClipboardText();
            if (string.IsNullOrWhiteSpace(copiedText) ||
                string.Equals(copiedText, sentinel, StringComparison.Ordinal))
            {
                Emit("ClipboardFallback", "NoSelection", "Clipboard still contained sentinel or empty text after Ctrl+C.");
                return null;
            }

            Emit("ClipboardFallback", "Success", $"Copied {copiedText.Trim().Length} characters from external target.");
            return new SelectionImportResult(
                copiedText.Trim(),
                "ekstern app via clipboard fallback",
                DateTimeOffset.Now);
        }
        finally
        {
            var clipboardRestored = RestoreClipboard(originalClipboard);
            Emit(
                "ClipboardFallback",
                clipboardRestored ? "ClipboardRestored" : "ClipboardRestoreFailed",
                originalClipboard is null ? "Original clipboard was empty or unavailable." : "Original clipboard data object was restored.");
            if (returnWindowHandle != IntPtr.Zero)
            {
                _ = SetForegroundWindow(returnWindowHandle);
            }
        }
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

            return focused is not null
                ? TryReadTextPatternSelection(focused, "Windows UI Automation focused descendant")
                : TryReadTextPatternSelection(root, "Windows UI Automation window");
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static SelectionImportResult? TryReadFocusedSelection(string source)
    {
        try
        {
            return TryReadTextPatternSelection(AutomationElement.FocusedElement, source);
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }
        catch (COMException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private static SelectionImportResult? TryReadTextPatternSelection(AutomationElement? element, string source)
    {
        if (element is null ||
            !element.TryGetCurrentPattern(TextPattern.Pattern, out var patternObject) ||
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
            : new SelectionImportResult(text, source, DateTimeOffset.Now);
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

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

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
}
