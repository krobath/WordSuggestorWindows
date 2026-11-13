using System.Text.Json;

internal static class DesktopEnvironmentSmoke
{
    internal static int Run()
    {
        var count = 0;
        void Check(string name, bool value) { if (!value) throw new Exception("Desktop probe: " + name); count++; Console.WriteLine("PASS DESKTOP " + name); }
        var family = DesktopEnvironmentProbe.InstallerFamily;
        var windows = new[] {
            new DesktopEnvironmentProbe.WindowIdentity(10, 100, true, family),
            new DesktopEnvironmentProbe.WindowIdentity(10, 101, true, family),
            new DesktopEnvironmentProbe.WindowIdentity(20, 200, false, family),
            new DesktopEnvironmentProbe.WindowIdentity(30, 300, true, "Other.App_8wekyb3d8bbwe"),
            new DesktopEnvironmentProbe.WindowIdentity(40, 400, true, null) };
        var state = DesktopEnvironmentProbe.Classify(windows, 10, 101);
        Check("visible frame and installer children count as one window", state.visible_installer_windows == 1);
        Check("installer foreground includes hosted child identity", state.installer_foreground && state.foreground_process_id == 101);
        state = DesktopEnvironmentProbe.Classify(windows, 40, 400);
        Check("background installer still blocks GUI", state.visible_installer_windows == 1 && !state.installer_foreground);
        state = DesktopEnvironmentProbe.Classify(windows.Skip(2), 30, 300);
        Check("hidden and unrelated packages are not installers", state.visible_installer_windows == 0 && !state.installer_foreground);
        state = DesktopEnvironmentProbe.Classify(Array.Empty<DesktopEnvironmentProbe.WindowIdentity>(), 0, 0);
        Check("empty or switching desktop does not invent a foreground", !state.installer_foreground && state.foreground_process_id == 0);
        var json = JsonSerializer.Serialize(state);
        Check("probe exposes fixed metadata fields only", JsonDocument.Parse(json).RootElement.EnumerateObject().Select(p => p.Name).SequenceEqual(new[] { "schema_version", "visible_installer_windows", "installer_foreground", "foreground_process_id" }));
        state = DesktopEnvironmentProbe.Read();
        Check("real read-only native probe returns bounded structural state", state.schema_version == 1 && state.visible_installer_windows is >= 0 and <= 256);
        return count;
    }
}
