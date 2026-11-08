using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WordSuggestorWindows.App.Services;

// All coordinates at this boundary are physical screen pixels, including
// negative origins. WPF DIPs are converted only for the target monitor's size.
public static class ScreenPlacement
{
    public static Point Compute(Rect workArea, Point anchor, Size size, bool followCaret)
    {
        var left = followCaret ? anchor.X - size.Width / 2 : anchor.X;
        var top = followCaret ? anchor.Y + 10 : anchor.Y;
        if (followCaret && top + size.Height > workArea.Bottom - 8) top = anchor.Y - size.Height - 18;
        return new(Math.Clamp(left, workArea.Left + 8, Math.Max(workArea.Left + 8, workArea.Right - size.Width - 8)),
            Math.Clamp(top, workArea.Top + 8, Math.Max(workArea.Top + 8, workArea.Bottom - size.Height - 8)));
    }

    public static Point TopLeft(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        return handle != IntPtr.Zero && GetWindowRect(handle, out var rect) ? new(rect.Left, rect.Top) : new(24, 88);
    }

    public static Point Place(Window window, Point physicalAnchor, bool followCaret)
    {
        var monitor = MonitorFromPoint(new POINT { X = (int)physicalAnchor.X, Y = (int)physicalAnchor.Y }, 2);
        var info = new MONITORINFO { Size = Marshal.SizeOf<MONITORINFO>() };
        if (!GetMonitorInfoW(monitor, ref info)) return physicalAnchor;
        uint dpiX = 96, dpiY = 96;
        if (GetDpiForMonitor(monitor, 0, out var x, out var y) == 0) { dpiX = x; dpiY = y; }
        var size = new Size(window.Width * dpiX / 96.0, window.Height * dpiY / 96.0);
        var work = new Rect(info.Work.Left, info.Work.Top, info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top);
        var position = Compute(work, physicalAnchor, size, followCaret);
        var handle = new WindowInteropHelper(window).EnsureHandle();
        SetWindowPos(handle, IntPtr.Zero, (int)Math.Round(position.X), (int)Math.Round(position.Y),
            (int)Math.Round(size.Width), (int)Math.Round(size.Height), 0x0010 | 0x0004);
        return position;
    }

    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct RECT { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MONITORINFO { public int Size; public RECT Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(POINT point, uint flags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfoW(IntPtr monitor, ref MONITORINFO info);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr handle, out RECT rect);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("shcore.dll")] private static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint dpiX, out uint dpiY);
}
