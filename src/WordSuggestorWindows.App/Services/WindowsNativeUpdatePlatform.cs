using System.Runtime.Versioning;
using System.Security.Principal;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsNativeUpdatePlatform : IWindowsUpdatePlatform
{
    private readonly InstalledRelease? _installed;
    private readonly SemaphoreSlim _mutation = new(1, 1);
    public WindowsNativeUpdatePlatform(InstalledRelease? installed = null) => _installed = installed ?? WindowsUpdateService.ReadInstalledRelease();
    public Task<WindowsUpdatePolicyResult> ReadAsync(CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (_installed is null) return Task.FromResult(new WindowsUpdatePolicyResult("not_installed"));
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return Task.FromResult(new WindowsUpdatePolicyResult("os_unsupported"));
        return Task.Run(() => OperatingSystem.IsWindowsVersionAtLeast(10,0,22000) ? ReadNative() : new WindowsUpdatePolicyResult("os_unsupported"), cancellation);
    }
    [SupportedOSPlatform("windows10.0.22000")]
    private WindowsUpdatePolicyResult ReadNative()
    {
        try
        {
            var package = OwnPackage(); var info = package.GetAppInstallerInfo();
            if (info is null) return new("no_association");
            var state = Snapshot(package, info);
            if (_installed!.Name != WindowsUpdateService.PackageName) return new("local_test", state);
            if (state.SourceUri != WindowsUpdateService.FeedUrl) return new("wrong_association", state);
            if (state.PolicySource != "Default") return new("system_managed", state);
            if (state.ForceAnyVersion || state.BlocksActivation) return new("policy_unsupported", state);
            return new(UpdatePolicyQualification.ProductionWriteQualified ? "ready" : "qualification_required", state, UpdatePolicyQualification.ProductionWriteQualified);
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException or UnauthorizedAccessException)
        { return new("os_unavailable", HResult: Code(e)); }
    }
    public async Task<NativeUpdateAvailability> CheckAsync(CancellationToken cancellation)
    {
        if (_installed is null) return new("not_installed");
        try
        {
            var package = OwnPackage();
            if (_installed.Name != WindowsUpdateService.PackageName || package.GetAppInstallerInfo()?.Uri.AbsoluteUri != WindowsUpdateService.FeedUrl) return new("wrong_association");
            // Microsoft documents that Package.Current.CheckUpdateAvailabilityAsync
            // can deny access. Reacquire through PackageManager.FindPackageForUser.
            var result = await package.CheckUpdateAvailabilityAsync().AsTask(cancellation);
            cancellation.ThrowIfCancellationRequested();
            return result.Availability switch
            {
                PackageUpdateAvailability.Available => new("available"), PackageUpdateAvailability.Required => new("required"),
                PackageUpdateAvailability.NoUpdates => new("current"), _ => new("os_unknown", result.ExtendedError is null ? null : Code(result.ExtendedError))
            };
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException or UnauthorizedAccessException)
        { return new("os_unavailable", Code(e)); }
    }
    public async Task<WindowsUpdateMutationResult> SetAsync(WindowsUpdatePolicy expected, bool enabled, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (!UpdatePolicyQualification.ProductionWriteQualified) return new("qualification_required", expected, false);
        if (!OperatingSystem.IsWindowsVersionAtLeast(10,0,22000)) return new("os_unsupported", expected, false);
        await _mutation.WaitAsync(cancellation);
        try { return await Task.Run(() => OperatingSystem.IsWindowsVersionAtLeast(10,0,22000) ? MutatePolicy(expected, enabled, cancellation, false) : new WindowsUpdateMutationResult("os_unsupported", expected, false), CancellationToken.None); }
        finally { _mutation.Release(); }
    }

    // Only the installed LocalTest app's explicit qualification activation may use
    // this path. Product UI cannot bypass the release qualification gate.
    internal async Task<WindowsUpdateMutationResult> SetFixtureAsync(WindowsUpdatePolicy expected, bool enabled, CancellationToken cancellation)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10,0,22000)) return new("os_unsupported", expected, false);
        await _mutation.WaitAsync(cancellation);
        try { return await Task.Run(() => OperatingSystem.IsWindowsVersionAtLeast(10,0,22000) ? MutatePolicy(expected, enabled, cancellation, true) : new WindowsUpdateMutationResult("os_unsupported", expected, false), CancellationToken.None); }
        finally { _mutation.Release(); }
    }
    internal async Task<WindowsUpdateMutationResult> RestoreFixtureAsync(WindowsUpdatePolicy original)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10,0,22000)) return new("os_unsupported", original, false);
        await _mutation.WaitAsync();
        try { return await Task.Run(() => OperatingSystem.IsWindowsVersionAtLeast(10,0,22000) ? RestoreNativeFixture(original) : new WindowsUpdateMutationResult("os_unsupported", original, false)); }
        finally { _mutation.Release(); }
    }
    [SupportedOSPlatform("windows10.0.22000")]
    private WindowsUpdateMutationResult RestoreNativeFixture(WindowsUpdatePolicy original)
    {
        try
        {
            var package = OwnPackage();
            var match = System.Text.RegularExpressions.Regex.Match(package.Id.Name, @"^WordSuggestor\.Windows\.LocalTest\.([a-f0-9]{8,32})$");
            if (!match.Success || package.Id.Publisher != "CN=WordSuggestor Local Test " + match.Groups[1].Value ||
                new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) return new("fixture_rejected", null, false);
            var info = package.GetAppInstallerInfo(); var now = Snapshot(package, info);
            if (now.NonSchedulingFingerprint != original.NonSchedulingFingerprint) return new("restore_failed", now, false);
            if (now.OnLaunch != original.OnLaunch || now.Background != original.Background)
            {
                var options = AutoUpdateSettingsOptions.CreateFromAppInstallerInfo(info);
                options.OnLaunch = original.OnLaunch; options.AutomaticBackgroundTask = original.Background;
                AppInstallerManager.GetDefault().SetAutoUpdateSettings(package.Id.FamilyName, options);
            }
            var restored = Snapshot(OwnPackage(), OwnPackage().GetAppInstallerInfo());
            return restored.NonSchedulingFingerprint == original.NonSchedulingFingerprint && restored.OnLaunch == original.OnLaunch && restored.Background == original.Background
                ? new("restored", restored, true) : new("restore_failed", restored, false);
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException or UnauthorizedAccessException)
        { return new("restore_failed", null, false, Code(e)); }
    }
    [SupportedOSPlatform("windows10.0.22000")]
    private WindowsUpdateMutationResult MutatePolicy(WindowsUpdatePolicy expected, bool enabled, CancellationToken cancellation, bool fixture)
    {
        var package = OwnPackage();
        var match = System.Text.RegularExpressions.Regex.Match(package.Id.Name, @"^WordSuggestor\.Windows\.LocalTest\.([a-f0-9]{8,32})$");
        if (fixture ? !match.Success || package.Id.Publisher != "CN=WordSuggestor Local Test " + match.Groups[1].Value
            : !UpdatePolicyQualification.ProductionWriteQualified || package.Id.Name != WindowsUpdateService.PackageName)
            return new("fixture_rejected", expected, false);
        if (new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)) return new("elevated", expected, false);
        var beforeInfo = package.GetAppInstallerInfo(); if (beforeInfo is null) return new("no_association", null, false);
        var before = Snapshot(package, beforeInfo);
        if (!fixture && (before.SourceUri != WindowsUpdateService.FeedUrl || before.ForceAnyVersion || before.BlocksActivation)) return new("policy_unsupported", before, false);
        if (before != expected && (before.NonSchedulingFingerprint != expected.NonSchedulingFingerprint || before.OnLaunch != expected.OnLaunch || before.Background != expected.Background)) return new("conflict", before, false);
        if (before.PolicySource != "Default") return new("system_managed", before, false);
        cancellation.ThrowIfCancellationRequested();
        var restoreOptions = AutoUpdateSettingsOptions.CreateFromAppInstallerInfo(beforeInfo);
        var manager = AppInstallerManager.GetDefault(); var attempted = false;
        try
        {
            var options = AutoUpdateSettingsOptions.CreateFromAppInstallerInfo(beforeInfo);
            options.OnLaunch = options.AutomaticBackgroundTask = enabled;
            attempted = true; manager.SetAutoUpdateSettings(package.Id.FamilyName, options);
            var after = Snapshot(OwnPackage(), OwnPackage().GetAppInstallerInfo());
            if (cancellation.IsCancellationRequested || after.NonSchedulingFingerprint != before.NonSchedulingFingerprint || after.OnLaunch != enabled || after.Background != enabled) throw new InvalidOperationException("OS policy verification failed.");
            return new("saved", after, false);
        }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException or UnauthorizedAccessException)
        {
            if (attempted)
            {
                try
                {
                    var now = Snapshot(OwnPackage(), OwnPackage().GetAppInstallerInfo());
                    if (now.NonSchedulingFingerprint != before.NonSchedulingFingerprint || now.OnLaunch != before.OnLaunch || now.Background != before.Background)
                        manager.SetAutoUpdateSettings(package.Id.FamilyName, restoreOptions);
                    var restored = Snapshot(OwnPackage(), OwnPackage().GetAppInstallerInfo());
                    if (restored.NonSchedulingFingerprint != before.NonSchedulingFingerprint || restored.OnLaunch != before.OnLaunch || restored.Background != before.Background) return new("restore_failed", restored, false, Code(e));
                }
                catch (Exception restore) when (restore is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException or UnauthorizedAccessException)
                { return new("restore_failed", null, false, Code(restore)); }
            }
            return new("write_failed", before, true, Code(e));
        }
    }
    private Package OwnPackage()
    {
        var current = Package.Current;
        if (_installed is null || current.Id.Name != _installed.Name || current.Id.Publisher != _installed.Publisher ||
            VersionOf(current.Id.Version) != _installed.Version || current.Id.Architecture.ToString() != "X64") throw new InvalidOperationException("Package context changed.");
        return new PackageManager().FindPackageForUser("", current.Id.FullName);
    }
    [SupportedOSPlatform("windows10.0.22000")]
    private static WindowsUpdatePolicy Snapshot(Package package, AppInstallerInfo info) => new(
        package.Id.FullName, package.Id.FamilyName, package.Id.Name, package.Id.Publisher, info.Uri.AbsoluteUri, VersionOf(info.Version),
        info.OnLaunch, info.AutomaticBackgroundTask, info.HoursBetweenUpdateChecks, info.ShowPrompt, info.UpdateBlocksActivation,
        info.ForceUpdateFromAnyVersion, info.IsAutoRepairEnabled, info.PolicySource.ToString(), info.UpdateUris.Select(u=>u.AbsoluteUri).ToArray(),
        info.RepairUris.Select(u=>u.AbsoluteUri).ToArray(), info.OptionalPackageUris.Select(u=>u.AbsoluteUri).ToArray(), info.DependencyPackageUris.Select(u=>u.AbsoluteUri).ToArray());
    internal static Version VersionOf(PackageVersion v) => new(v.Major,v.Minor,v.Build,v.Revision);
    private static string Code(Exception e) => "0x" + e.HResult.ToString("X8");
}
