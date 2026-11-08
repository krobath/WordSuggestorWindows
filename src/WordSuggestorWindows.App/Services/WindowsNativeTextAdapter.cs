using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WordSuggestorWindows.App.Services;

public sealed record ExternalTextSnapshot(IntPtr Window, IntPtr Control, uint ProcessId,
    long ProcessStarted, string Text, int Start, int End)
{
    public string Token => Start == End ? EditorCommit.Prefix(Text, End) : Text[Start..End];
    public override string ToString() => $"NativeEdit pid={ProcessId}, selectionLength={End - Start}";
}

// Qualified for Notepad native controls only. Never inject into an arbitrary
// foreground window; unsupported controls fail closed before reading content.
public sealed class WindowsNativeTextAdapter
{
    private const int MaxDocumentLength = 60_000; // EM_GETSEL has a 16-bit packed result.

    public ExternalTextSnapshot? ReadFocused()
    {
        try
        {
            var window = GetForegroundWindow();
            var thread = GetWindowThreadProcessId(window, out var pid);
            if (pid == 0 || pid == Environment.ProcessId) return null;
            using var process = Process.GetProcessById((int)pid);
            if (!process.ProcessName.Equals("notepad", StringComparison.OrdinalIgnoreCase)) return null;
            var info = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>() };
            if (!GetGUIThreadInfo(thread, ref info) || info.hwndFocus == IntPtr.Zero ||
                GetAncestor(info.hwndFocus, 2) != window) return null;
            var name = new StringBuilder(128);
            if (GetClassName(info.hwndFocus, name, name.Capacity) == 0) return null;
            var className = name.ToString();
            if (!className.Equals("Edit", StringComparison.OrdinalIgnoreCase) &&
                !className.Equals("RichEditD2DPT", StringComparison.OrdinalIgnoreCase) &&
                !className.Equals("RICHEDIT50W", StringComparison.OrdinalIgnoreCase)) return null;
            var style = GetWindowLongPtrW(info.hwndFocus, -16).ToInt64();
            if ((style & (0x20 | 0x800)) != 0) return null; // password/read-only
            if (!Send(info.hwndFocus, 0x000E, IntPtr.Zero, IntPtr.Zero, out var length) ||
                length.ToInt64() is < 0 or > MaxDocumentLength) return null;
            var buffer = new StringBuilder((int)length + 1);
            if (SendMessageTimeoutText(info.hwndFocus, 0x000D, new IntPtr(buffer.Capacity), buffer,
                    0x0002, 100, out var read) == IntPtr.Zero || read.ToInt64() != length.ToInt64()) return null;
            if (!Send(info.hwndFocus, 0x00B0, IntPtr.Zero, IntPtr.Zero, out var selection)) return null;
            var packed = selection.ToInt64();
            var start = (int)(packed & 0xffff);
            var end = (int)((packed >> 16) & 0xffff);
            if (start > end || end > buffer.Length || GetForegroundWindow() != window) return null;
            return new(window, info.hwndFocus, pid, process.StartTime.ToUniversalTime().Ticks, buffer.ToString(), start, end);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    public bool TryCommit(ExternalTextSnapshot expected, string typedToken, string replacement, out string diagnostic)
    {
        diagnostic = "NativeEdit.TargetChanged";
        var current = ReadFocused();
        if (current != expected || expected.Token != typedToken || string.IsNullOrEmpty(typedToken)) return false;
        var start = current.Start == current.End ? current.Start - typedToken.Length : current.Start;
        var end = current.End;
        if (current.Start == current.End)
            while (end < current.Text.Length && EditorCommit.IsTokenCharacter(current.Text[end])) end++;
        var insertion = replacement + (end < current.Text.Length && char.IsWhiteSpace(current.Text[end]) ? "" : " ");
        if (!Send(current.Control, 0x00B1, new IntPtr(start), new IntPtr(end), out _)) return false;
        var selected = ReadFocused();
        if (selected != current with { Start = start, End = end }) return false;
        // USER32 marshals EM_REPLACESEL to this exact control; clipboard, undo and
        // Unicode are preserved. No SendInput and no automatic fallback/retry.
        if (SendMessageTimeoutString(current.Control, 0x00C2, new IntPtr(1), insertion,
                0x0002, 250, out _) == IntPtr.Zero)
        {
            diagnostic = "NativeEdit.CommitUnconfirmed";
            return false;
        }
        var after = ReadFocused();
        var wanted = current.Text[..start] + insertion + current.Text[end..];
        var caret = start + insertion.Length;
        if (after is null || after.Control != current.Control || after.Text != wanted || after.Start != caret || after.End != caret)
        {
            diagnostic = "NativeEdit.CommitUnconfirmed";
            return false;
        }
        diagnostic = "NativeEdit.Verified";
        return true;
    }

    public static bool IsOwnWindow(IntPtr window)
    {
        GetWindowThreadProcessId(window, out var pid);
        return pid == Environment.ProcessId;
    }

    private static bool Send(IntPtr control, uint message, IntPtr wParam, IntPtr lParam, out IntPtr result) =>
        SendMessageTimeoutW(control, message, wParam, lParam, 0x0002, 100, out result) != IntPtr.Zero;
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct GUITHREADINFO
    {
        public uint cbSize, flags;
        public IntPtr hwndActive, hwndFocus, hwndCapture, hwndMenuOwner, hwndMoveSize, hwndCaret;
        public RECT rcCaret;
    }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DllImport("user32.dll")] private static extern bool GetGUIThreadInfo(uint thread, ref GUITHREADINFO info);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder buffer, int length);
    [DllImport("user32.dll")] private static extern IntPtr GetWindowLongPtrW(IntPtr window, int index);
    [DllImport("user32.dll")] private static extern IntPtr SendMessageTimeoutW(IntPtr window, uint message, IntPtr wParam, IntPtr lParam, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessageTimeoutText(IntPtr window, uint message, IntPtr wParam, [Out] StringBuilder buffer, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessageTimeoutString(IntPtr window, uint message, IntPtr wParam, string text, uint flags, uint timeout, out IntPtr result);
}
