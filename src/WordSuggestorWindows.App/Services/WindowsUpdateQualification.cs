using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;

namespace WordSuggestorWindows.App.Services;

public static class WindowsUpdateQualification
{
    public static async Task<int?> TryRunAsync(IReadOnlyList<string> args)
    {
        if (!args.Contains("--qualify-update-policy")) return null;
        if (args.Count != 2 || args[0] != "--qualify-update-policy" || !System.Text.RegularExpressions.Regex.IsMatch(args[1], "^[a-f0-9]{32}$")) return 2;
        var installed = WindowsUpdateService.ReadInstalledRelease();
        var match = System.Text.RegularExpressions.Regex.Match(installed?.Name ?? "", @"^WordSuggestor\.Windows\.LocalTest\.([a-f0-9]{8,32})$");
        if (!match.Success || installed!.Publisher != "CN=WordSuggestor Local Test " + match.Groups[1].Value) return 3;
        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WordSuggestor", "LocalTests", match.Groups[1].Value, "diagnostics");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "update-policy-" + args[1] + ".json");
        var platform = new WindowsNativeUpdatePlatform(installed);
        var before = await platform.ReadAsync(CancellationToken.None);
        var status = "blocked"; var changed = false; var restored = true; string? hresult = before.HResult;
        if (before.Policy is { } policy)
        {
            try
            {
                var result = await platform.SetFixtureAsync(policy, !(policy.OnLaunch && policy.Background), CancellationToken.None);
                hresult = result.HResult;
                changed = result.Code == "saved" && result.Policy is { } after && after.NonSchedulingFingerprint == policy.NonSchedulingFingerprint &&
                    (after.OnLaunch != policy.OnLaunch || after.Background != policy.Background);
            }
            finally
            {
                var restore = await platform.RestoreFixtureAsync(policy); restored = restore.Restored;
                if (!restored) hresult = restore.HResult;
            }
            status = !restored ? "restore_failed" : changed ? "passed" : "blocked";
        }
        using var assembly = File.OpenRead(typeof(WindowsUpdateQualification).Assembly.Location);
        var evidence = new { schema_version = 1, status, run_id = args[1], package_name = installed.Name,
            package_version = installed.Version.ToString(), binary_sha256 = Convert.ToHexString(SHA256.HashData(assembly)),
            os_version = Environment.OSVersion.Version.ToString(), packaged_app_context = true, changed, restored,
            source_and_other_settings_preserved = restored, hresult, production_control_enabled = false };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
        return status == "passed" ? 0 : status == "blocked" ? 3 : 1;
    }
}
