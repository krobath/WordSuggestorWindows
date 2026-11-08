using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WordSuggestorWindows.App.Services;

internal static class NonActivatingWindow
{
    public static void Apply(Window window)
    {
        window.ShowActivated = false;
        window.SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(window).Handle;
            var style = GetWindowLongPtrW(handle, -20).ToInt64();
            SetWindowLongPtrW(handle, -20, new IntPtr(style | 0x08000000 | 0x00000080));
            HwndSource.FromHwnd(handle)?.AddHook(PreventActivation);
        };
    }

    private static IntPtr PreventActivation(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != 0x0021) return IntPtr.Zero;
        handled = true;
        return new IntPtr(3); // MA_NOACTIVATE: deliver click, keep the typing target active.
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtrW(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtrW(IntPtr hwnd, int index, IntPtr value);
}
