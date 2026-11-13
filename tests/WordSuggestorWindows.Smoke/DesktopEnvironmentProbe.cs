using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

// Test-only, read-only inspection. Never read window titles/document text or
// invoke, dismiss, kill or change any App Installer window/process.
internal static class DesktopEnvironmentProbe
{
    internal const string InstallerFamily = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe";
    internal sealed record WindowIdentity(nint Root, uint ProcessId, bool Visible, string? Family);
    internal sealed record State(int schema_version, int visible_installer_windows, bool installer_foreground, uint foreground_process_id);

    internal static State Classify(IEnumerable<WindowIdentity> windows, nint foreground, uint foregroundProcess)
    {
        var roots = windows.Where(w => w.Visible && w.Family == InstallerFamily).Select(w => w.Root).Distinct().ToArray();
        return new(1, roots.Length, roots.Contains(foreground), foregroundProcess);
    }

    internal static State Read()
    {
        var windows = new List<WindowIdentity>();
        var families = new Dictionary<uint, string?>();
        string? Family(uint pid)
        {
            if (families.TryGetValue(pid, out var cached)) return cached;
            var process = OpenProcess(0x1000, false, pid); // QUERY_LIMITED_INFORMATION
            if (process == 0) return families[pid] = null;
            try
            {
                uint length = 256;
                var buffer = new StringBuilder((int)length);
                return families[pid] = GetPackageFamilyName(process, ref length, buffer) == 0 ? buffer.ToString() : null;
            }
            finally { CloseHandle(process); }
        }
        void Inspect(nint root, nint window)
        {
            GetWindowThreadProcessId(window, out var pid);
            windows.Add(new(root, pid, IsWindowVisible(window), Family(pid)));
        }
        EnumWindows((window, _) =>
        {
            if (!IsWindowVisible(window)) return true;
            if (DwmGetWindowAttribute(window, 14, out var cloaked, sizeof(int)) == 0 && cloaked != 0) return true;
            Inspect(window, window);
            // Older Windows versions can host packaged UIs in a frame process.
            var name = new StringBuilder(128);
            GetClassName(window, name, name.Capacity);
            if (name.ToString() == "ApplicationFrameWindow")
                EnumChildWindows(window, (child, _) => { Inspect(window, child); return true; }, 0);
            return true;
        }, 0);
        var foreground = GetForegroundWindow();
        GetWindowThreadProcessId(foreground, out var foregroundProcess);
        return Classify(windows, foreground, foregroundProcess);
    }

    internal static void AssertInstallerIdle(string phase)
    {
        var state = Read();
        if (state.visible_installer_windows != 0)
            throw new InvalidOperationException($"GUI.EnvironmentBlocked.AppInstaller phase={phase} windows={state.visible_installer_windows} installer_foreground={state.installer_foreground} foreground_pid={state.foreground_process_id}. Close the test update dialog with Cancel; no input was sent.");
    }

    internal static int PrintState()
    {
        try { Console.WriteLine(JsonSerializer.Serialize(Read())); return 0; }
        catch (Exception ex) { Console.Error.WriteLine("DESKTOP_PROBE_FAILED " + ex.GetType().Name); return 1; }
    }

    private delegate bool EnumWindow(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindow callback, nint state);
    [DllImport("user32.dll")] private static extern bool EnumChildWindows(nint parent, EnumWindow callback, nint state);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder name, int count);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
    [DllImport("kernel32.dll")] private static extern nint OpenProcess(uint access, bool inherit, uint process);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)] private static extern int GetPackageFamilyName(nint process, ref uint length, StringBuilder name);
}
