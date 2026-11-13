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
        string? mutationCode = null, mutationStage = "read_policy", restoreCode = null, restoreStage = null;
        var steps = new List<object>();
        if (before.Policy is { } policy)
        {
            try
            {
                var expected = policy;
                var firstEnabled = !(policy.OnLaunch && policy.Background);
                var allVerified = true;
                foreach (var enabled in new[] { firstEnabled, !firstEnabled })
                {
                    var result = await platform.SetFixtureAsync(expected, enabled, CancellationToken.None);
                    hresult = result.HResult; mutationCode = result.Code; mutationStage = result.Stage ?? result.Code;
                    var verified = result.Code == "saved" && result.Policy is { } after &&
                        after.NonSchedulingFingerprint == policy.NonSchedulingFingerprint && after.OnLaunch == enabled && after.Background == enabled;
                    steps.Add(new { enabled, verified, code = result.Code, stage = result.Stage, hresult = result.HResult });
                    if (!verified) { allVerified = false; break; }
                    expected = result.Policy!;
                }
                changed = allVerified && steps.Count == 2;
            }
            finally
            {
                var restore = await platform.RestoreFixtureAsync(policy); restored = restore.Restored;
                restoreCode = restore.Code; restoreStage = restore.Stage;
                if (!restored) hresult = restore.HResult;
            }
            status = !restored ? "restore_failed" : changed ? "passed" : "blocked";
        }
        using var assembly = File.OpenRead(typeof(WindowsUpdateQualification).Assembly.Location);
        var nativeControl = await RunNativeControlAsync();
        if (nativeControl is { } control && !control.GetProperty("restored").GetBoolean()) { restored = false; status = "restore_failed"; }
        var manifestSettings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null };
        using var manifestReader = System.Xml.XmlReader.Create(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "AppxManifest.xml"), manifestSettings);
        var manifest = System.Xml.Linq.XDocument.Load(manifestReader);
        var capabilities = manifest.Descendants(System.Xml.Linq.XName.Get("Capability", "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"))
            .Select(e => (string?)e.Attribute("Name")).ToHashSet();
        var evidence = new { schema_version = 1, status, run_id = args[1], package_name = installed.Name,
            package_version = installed.Version.ToString(), binary_sha256 = Convert.ToHexString(SHA256.HashData(assembly)),
            os_version = Environment.OSVersion.Version.ToString(), packaged_app_context = true, changed, restored,
            source_and_other_settings_preserved = restored, hresult, production_control_enabled = false,
            read_code = before.Code, mutation_code = mutationCode, mutation_stage = mutationStage, restore_code = restoreCode, restore_stage = restoreStage,
            package_management_declared = capabilities.Contains("packageManagement"), package_query_declared = capabilities.Contains("packageQuery"), steps, native_control = nativeControl };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
        return status == "passed" ? 0 : status == "blocked" ? 3 : 1;
    }

    private static async Task<JsonElement?> RunNativeControlAsync()
    {
        var package = Windows.ApplicationModel.Package.Current;
        var path = Path.Combine(package.InstalledLocation.Path, "Diagnostics", "UpdatePolicyControl.exe");
        if (!File.Exists(path)) return null; // absent in ordinary/production packages
        var start = new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(package.Id.FamilyName);
        using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("Native policy control unavailable.");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        async Task<string> ReadBounded(StreamReader reader)
        {
            var result = new System.Text.StringBuilder(); var buffer = new char[256]; int read;
            while ((read = await reader.ReadAsync(buffer.AsMemory(), timeout.Token)) > 0)
            { if (result.Length + read > 8192) throw new InvalidDataException("Native control output exceeded limit."); result.Append(buffer, 0, read); }
            return result.ToString();
        }
        try
        {
            var output = ReadBounded(process.StandardOutput); var error = ReadBounded(process.StandardError);
            await process.WaitForExitAsync(timeout.Token);
            var raw = await output; if ((await error).Length != 0 || process.ExitCode is not (0 or 1 or 3)) throw new InvalidDataException("Native control process failed.");
            return ReadNativeControlEvidence(raw);
        }
        finally { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } }
    }

    internal static JsonElement ReadNativeControlEvidence(string raw)
    {
        if (raw.Length > 8192) throw new InvalidDataException("Native control output exceeded limit.");
        using var document = JsonDocument.Parse(raw); var value = document.RootElement;
        var names = new[] { "schema_version", "packaged_context_matches", "noop_verified", "off_verified", "on_verified", "restored", "failure_stage", "hresult", "factory_hresult", "manual_options", "info_route", "input_cases" };
        if (value.EnumerateObject().Count() != names.Length || names.Any(n => !value.TryGetProperty(n, out _)) || value.GetProperty("schema_version").GetInt32() != 3 || value.GetProperty("info_route").GetString() != "current_package")
            throw new InvalidDataException("Native control schema rejected.");
        foreach (var name in names.Skip(1).Take(5)) { _ = value.GetProperty(name).GetBoolean(); }
        _ = value.GetProperty("manual_options").GetBoolean();
        if (!System.Text.RegularExpressions.Regex.IsMatch(value.GetProperty("hresult").GetString() ?? "", "^0x[A-F0-9]{8}$") ||
            !System.Text.RegularExpressions.Regex.IsMatch(value.GetProperty("factory_hresult").GetString() ?? "", "^0x[A-F0-9]{8}$") ||
            value.GetProperty("failure_stage").GetString() is not ("none" or "identity" or "read_policy" or "create_options" or "create_factory_noop" or "create_manual_noop" or "write_noop" or "write_manual_noop" or "write_off" or "write_on"))
            throw new InvalidDataException("Native control metadata rejected.");
        if ((value.GetProperty("off_verified").GetBoolean() && !value.GetProperty("noop_verified").GetBoolean()) ||
            (value.GetProperty("on_verified").GetBoolean() && !value.GetProperty("off_verified").GetBoolean()) ||
            (value.GetProperty("hresult").GetString() == "0x00000000" &&
            (!value.GetProperty("packaged_context_matches").GetBoolean() || !value.GetProperty("on_verified").GetBoolean() || value.GetProperty("failure_stage").GetString() != "none")))
            throw new InvalidDataException("Native control outcomes inconsistent.");
        var cases = value.GetProperty("input_cases");
        var caseNames = new[] { "no_prompt", "minimal", "minimal_no_version", "update_uri", "repair_uri", "both_uris", "no_repair" };
        if (cases.GetArrayLength() > caseNames.Length || (cases.GetArrayLength() > 0 && !value.GetProperty("packaged_context_matches").GetBoolean())) throw new InvalidDataException("Input cases rejected.");
        var index = 0;
        foreach (var item in cases.EnumerateArray())
        {
            var fields = new[] { "name", "accepted", "restored", "stage", "hresult" };
            if (item.EnumerateObject().Count() != fields.Length || fields.Any(n => !item.TryGetProperty(n, out _)) || item.GetProperty("name").GetString() != caseNames[index++]) throw new InvalidDataException("Input case schema rejected.");
            var accepted = item.GetProperty("accepted").GetBoolean(); var restored = item.GetProperty("restored").GetBoolean();
            var stage = item.GetProperty("stage").GetString(); var code = item.GetProperty("hresult").GetString() ?? "";
            if (stage is not ("create_options" or "write_policy" or "read_after" or "completed") || !System.Text.RegularExpressions.Regex.IsMatch(code, "^0x[A-F0-9]{8}$") ||
                (code == "0x00000000" && (!accepted || stage != "completed")) ||
                (stage == "completed" && (!accepted || code != "0x00000000")) ||
                (!restored && value.GetProperty("restored").GetBoolean())) throw new InvalidDataException("Input case outcomes rejected.");
        }
        return value.Clone();
    }
}
