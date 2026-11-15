using System.ComponentModel;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsUpdateCoordinator : INotifyPropertyChanged, IDisposable
{
    private readonly WindowsUpdateService _metadata;
    private readonly IWindowsUpdatePlatform _platform;
    private readonly IWindowsInstallerLauncher _launcher;
    private readonly TimeProvider _clock;
    private readonly WindowsExeUpdateService? _direct;
    private ExeUpdateOffer? _directOffer;
    private readonly CancellationTokenSource _lifetime = new();
    private long _revision;
    private bool _disposed;
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool Busy { get; private set; }
    public bool IsDirectInstalled => _direct is not null;
    public bool DirectChecksEnabled => _direct?.Installation.UpdatesEnabled == true;
    public string VersionLabel => _direct is { } direct ? SettingsText.Format("VersionInstalled", direct.Installation.DisplayVersion, direct.Installation.PackageVersion) : _metadata.VersionLabel;
    public string StatusCode { get; private set; } = "idle";
    public WindowsUpdatePolicyResult PolicyState { get; private set; } = new("loading");
    public Version? AvailableVersion { get; private set; }
    public bool HasOffer => AvailableVersion is not null && StatusCode is "available" or "required";
    public WindowsUpdateCoordinator(WindowsUpdateService? metadata = null, IWindowsUpdatePlatform? platform = null, IWindowsInstallerLauncher? launcher = null, TimeProvider? clock = null, WindowsExeUpdateService? direct = null)
    { _metadata = metadata ?? new(); _platform = platform ?? new WindowsNativeUpdatePlatform(); _launcher = launcher ?? new WindowsInstallerLauncher(); _clock = clock ?? TimeProvider.System;
      _direct = direct ?? (metadata is null && platform is null && launcher is null && WindowsUpdateService.ReadInstalledRelease() is null && DirectInstallation.Current is { } installed ? new WindowsExeUpdateService(installed) : null); }
    public Task<bool> RefreshAsync() => Run(async token =>
    { if (_direct is not null) { PolicyState = new(DirectChecksEnabled ? "exe_confirmation" : "exe_preview"); return; } var state = await _platform.ReadAsync(token); token.ThrowIfCancellationRequested(); PolicyState = state; });
    public Task<bool> SetAutomaticAsync(bool enabled) => Run(async token =>
    {
        if (!PolicyState.CanChange || PolicyState.Policy is not { } expected) { StatusCode = "policy_readonly"; return; }
        AvailableVersion = null;
        var changed = await _platform.SetAsync(expected, enabled, token); token.ThrowIfCancellationRequested();
        // Do not infer success from a Set return. Re-read actual OS state.
        var readback = await _platform.ReadAsync(token); token.ThrowIfCancellationRequested(); PolicyState = readback;
        if (changed.Code != "saved" || readback.Policy is not { } actual || actual.OnLaunch != enabled || actual.Background != enabled || actual.NonSchedulingFingerprint != expected.NonSchedulingFingerprint)
        { StatusCode = changed.Code == "restore_failed" ? "restore_failed" : "policy_write_failed"; return; }
        StatusCode = "policy_saved";
    });
    public Task<bool> CheckAsync() => Run(async token =>
    { await CheckCore(token); });
    private async Task CheckCore(CancellationToken token)
    {
        AvailableVersion = null; StatusCode = "checking"; Notify();
        if (_direct is not null)
        {
            var result = await _direct.CheckAsync(token); token.ThrowIfCancellationRequested();
            _directOffer = result.Offer; AvailableVersion = result.Offer?.PackageVersion; StatusCode = result.Code; return;
        }
        var metadata = await _metadata.CheckAsync(token); token.ThrowIfCancellationRequested();
        if (metadata.Code is not ("available" or "current")) { StatusCode = metadata.Code; return; }
        var native = await _platform.CheckAsync(token); token.ThrowIfCancellationRequested();
        if (metadata.Code == "available" && native.Code is "available" or "required")
        { AvailableVersion = metadata.AvailableVersion; StatusCode = native.Code; }
        else StatusCode = metadata.Code == "current" && native.Code == "current" ? "current" : native.Code is "current" or "available" or "required" ? "pending_os" : native.Code;
    }
    public async Task<VerifiedUpdateHandoff?> PrepareHandoffAsync()
    {
        VerifiedUpdateHandoff? ticket = null;
        var valid = await Run(async token =>
        {
            // Revalidate mutable public feed and the current package's OS
            // association immediately before an explicit user-approved handoff.
            var confirmedOffer = _directOffer;
            await CheckCore(token); token.ThrowIfCancellationRequested();
            if (_direct is not null && (confirmedOffer is null || confirmedOffer != _directOffer))
            { AvailableVersion = null; StatusCode = "offer_changed"; return; }
            if (HasOffer && AvailableVersion is { } version)
            {
                var launcher = _direct is not null && _directOffer is { } offer ? await _direct.DownloadVerifiedAsync(offer, token) : _launcher;
                token.ThrowIfCancellationRequested(); ticket = new(launcher, version, _clock);
            }
        });
        return valid ? ticket : null;
    }
    private async Task<bool> Run(Func<CancellationToken, Task> action)
    {
        if (_disposed || Busy) return false;
        Busy = true; var revision = ++_revision; var token = _lifetime.Token; Notify();
        try { await action(token); token.ThrowIfCancellationRequested(); return !_disposed && revision == _revision; }
        catch (OperationCanceledException) { return false; }
        catch (Exception e) when (e is InvalidOperationException or ArgumentException or System.Runtime.InteropServices.COMException or UnauthorizedAccessException or System.IO.IOException or System.Net.Http.HttpRequestException)
        { if (!_disposed && revision == _revision) { AvailableVersion = null; StatusCode = IsDirectInstalled ? "installer_unavailable" : "os_unavailable"; } return false; }
        finally { Busy = false; if (!_disposed) Notify(); }
    }
    private void Notify() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    public void Dispose() { if (_disposed) return; _disposed = true; _revision++; _lifetime.Cancel(); _metadata.Dispose(); _direct?.Dispose(); _lifetime.Dispose(); }
}

public sealed class WindowsInstallerLauncher : IWindowsInstallerLauncher
{
    public void Open() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
        "ms-appinstaller:?source=" + Uri.EscapeDataString(WindowsUpdateService.FeedUrl)) { UseShellExecute = true });
}
