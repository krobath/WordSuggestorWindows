using WordSuggestorWindows.App.Services;

namespace WordSuggestorWindows.App.Models;

public sealed record WindowsUpdatePolicy(
    string PackageFullName, string FamilyName, string Name, string Publisher, string SourceUri, Version InstallerVersion,
    bool OnLaunch, bool Background, uint HoursBetweenChecks, bool ShowPrompt, bool BlocksActivation,
    bool ForceAnyVersion, bool AutoRepair, string PolicySource, string[] UpdateUris, string[] RepairUris,
    string[] OptionalUris, string[] DependencyUris)
{
    [System.Text.Json.Serialization.JsonIgnore] public bool? Automatic => OnLaunch == Background ? OnLaunch : null;
    [System.Text.Json.Serialization.JsonIgnore] public string NonSchedulingFingerprint => System.Text.Json.JsonSerializer.Serialize(this with { OnLaunch = false, Background = false });
    public override string ToString() => $"WindowsUpdatePolicy[source={PolicySource}, onLaunch={OnLaunch}, background={Background}]";
}
public sealed record WindowsUpdatePolicyResult(string Code, WindowsUpdatePolicy? Policy = null, bool CanChange = false, string? HResult = null);
public sealed record WindowsUpdateMutationResult(string Code, WindowsUpdatePolicy? Policy, bool Restored, string? HResult = null, string? Stage = null);
public sealed record NativeUpdateAvailability(string Code, string? HResult = null);

public interface IWindowsUpdatePlatform
{
    Task<WindowsUpdatePolicyResult> ReadAsync(CancellationToken cancellation);
    Task<WindowsUpdateMutationResult> SetAsync(WindowsUpdatePolicy expected, bool enabled, CancellationToken cancellation);
    Task<NativeUpdateAvailability> CheckAsync(CancellationToken cancellation);
}

public interface IWindowsInstallerLauncher { void Open(); }

public sealed class VerifiedUpdateHandoff
{
    private readonly IWindowsInstallerLauncher _launcher;
    private readonly TimeProvider _clock;
    private readonly long _created;
    private int _used;
    internal VerifiedUpdateHandoff(IWindowsInstallerLauncher launcher, Version version, TimeProvider clock) { _launcher = launcher; Version = version; _clock = clock; _created = clock.GetTimestamp(); }
    public Version Version { get; }
    // One use, monotonic lifetime. No raw URI or command can be substituted.
    public bool TryOpen()
    {
        if (_clock.GetElapsedTime(_created) > TimeSpan.FromMinutes(2) || Interlocked.Exchange(ref _used, 1) != 0) return false;
        _launcher.Open(); return true;
    }
}

internal static class UpdatePolicyQualification
{
    // Release gate: native G05 mutation still fails; installed tests now run.
    // Flip only in a reviewed change with native packaged read/change/read/restore evidence.
    public static readonly bool ProductionWriteQualified = false;
}
