using System.Windows;

namespace WordSuggestorWindows.App.Services;

public interface IWindowsUpdateHandoff { bool CloseThenLaunch(Window requestingWindow, Action launch); }

public sealed class WindowsUpdateHandoff : IWindowsUpdateHandoff
{
    public bool CloseThenLaunch(Window requestingWindow, Action launch)
    {
        var root = requestingWindow;
        while (root.Owner is { } parent) root = parent;
        if (root is not MainWindow) return false;
        // Existing application Closing handlers own saving, dirty-tab/file
        // prompts and cancellation. No direct Shutdown/kill/forced restart.
        var closed = false;
        void Closed(object? sender, EventArgs e) { closed = true; root.Closed -= Closed; launch(); }
        root.Closed += Closed;
        try { root.Close(); }
        finally { if (!closed) root.Closed -= Closed; }
        return closed;
    }
}
