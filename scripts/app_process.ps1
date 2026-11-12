# Only normal window-close requests are allowed here. Never terminate an editor
# process: the application owns save/cancel prompts for unsaved documents.
function Resolve-WordSuggestorDevPath([string]$Path) {
    if (-not [IO.Path]::IsPathRooted($Path)) {
        $location = Get-Location
        if ($location.Provider.Name -ne 'FileSystem') { throw 'Development paths require a filesystem working directory.' }
        $Path = Join-Path $location.ProviderPath $Path
    }
    return [IO.Path]::GetFullPath($Path)
}

function Get-WordSuggestorAppProcess([string]$AppPath) {
    $expected = Resolve-WordSuggestorDevPath $AppPath
    foreach ($candidate in [Diagnostics.Process]::GetProcessesByName([IO.Path]::GetFileNameWithoutExtension($expected))) {
        try {
            if (-not $candidate.HasExited -and [string]::Equals($candidate.MainModule.FileName, $expected, [StringComparison]::OrdinalIgnoreCase)) { $candidate }
        } catch [ComponentModel.Win32Exception] {
            throw 'Cannot inspect a running app process. Close WordSuggestor normally before rebuilding.'
        }
    }
}

function Close-WordSuggestorAppGracefully([Diagnostics.Process]$AppProcess, [string]$ExpectedPath, [int]$TimeoutSeconds = 120) {
    if ($AppProcess.HasExited) { return }
    $current = [Diagnostics.Process]::GetProcessById($AppProcess.Id)
    if ($current.StartTime -ne $AppProcess.StartTime -or
        -not [string]::Equals($current.MainModule.FileName, (Resolve-WordSuggestorDevPath $ExpectedPath), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Running process identity changed; restart cancelled.'
    }
    if (-not ('WordSuggestor.DevAppClose' -as [type])) {
        Add-Type -TypeDefinition @'
using System;
using System.Text;
using System.Runtime.InteropServices;
namespace WordSuggestor {
    public static class DevAppClose {
        private delegate bool EnumWindow(IntPtr window, IntPtr state);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindow callback, IntPtr state);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int length);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wparam, IntPtr lparam);
        public static bool Request(int processId) {
            bool requested = false;
            EnumWindows(delegate(IntPtr window, IntPtr state) {
                uint owner; GetWindowThreadProcessId(window, out owner);
                if (owner != (uint)processId) return true;
                var title = new StringBuilder(256); GetWindowText(window, title, title.Capacity);
                // The toolbar is a tool window, so Process.MainWindowHandle is
                // normally zero. Closing an editor would only hide it.
                if (title.ToString() != "WordSuggestor Windows") return true;
                requested = PostMessage(window, 0x0010, IntPtr.Zero, IntPtr.Zero);
                return false;
            }, IntPtr.Zero);
            return requested;
        }
    }
}
'@
    }
    if (-not [WordSuggestor.DevAppClose]::Request($current.Id)) {
        if ($current.HasExited) { return }
        throw 'Cannot request normal shutdown. Use Afslut WordSuggestor in the app; no process was terminated.'
    }
    Write-Host 'Waiting for WordSuggestor to exit. Respond to any save prompt in the app; Cancel keeps it running.'
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while (-not $current.WaitForExit(250)) {
        if ([DateTime]::UtcNow -ge $deadline) {
            throw 'WordSuggestor is still running (shutdown cancelled or save prompt pending). Build/restart stopped; no process was terminated.'
        }
    }
}
