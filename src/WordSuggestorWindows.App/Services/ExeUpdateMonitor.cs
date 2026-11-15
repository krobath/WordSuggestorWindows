using System.Windows;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

// Checks in the background; offers only when a normal WordSuggestor window is
// active, so a floating toolbar never steals another application's typing focus.
public sealed class ExeUpdateMonitor : IDisposable
{
    private readonly Application _app;
    private readonly WindowsExeUpdateService _service;
    private readonly WindowsAppSettingsService _settings = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly HashSet<Version> _offered = [];
    private ExeUpdateOffer? _pending;
    private bool _disposed;
    public ExeUpdateMonitor(Application app, DirectInstallation installation)
    { _app = app; _service = new(installation); _app.Activated += Activated; _ = PollAsync(_lifetime.Token); }
    private async Task PollAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3), token);
            using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
            do
            {
                if (_settings.Load().CheckExeUpdatesAutomatically)
                {
                    var result = await _service.CheckAsync(token);
                    _pending = result.Code == "available" ? result.Offer : null;
                    if (_pending is not null) await OfferAsync(token);
                }
            } while (await timer.WaitForNextTickAsync(token));
        }
        catch (OperationCanceledException) { }
        catch (Exception e) when (e is InvalidOperationException or System.IO.IOException or UnauthorizedAccessException) { }
    }
    private async void Activated(object? sender, EventArgs e)
    { if (_disposed) return; try { await OfferAsync(_lifetime.Token); } catch (OperationCanceledException) { } }
    private async Task OfferAsync(CancellationToken token)
    {
        if (_disposed || _pending is not { } offer || _offered.Contains(offer.PackageVersion) || !_settings.Load().CheckExeUpdatesAutomatically) return;
        var owner = _app.Windows.Cast<Window>().FirstOrDefault(w => w.IsActive && w is InternalEditorWindow or SettingsWindow or LegacySettingsWindow);
        if (owner is null || !ExeUpdateInteraction.TryEnter()) return;
        try
        {
            if (_disposed) return;
            _offered.Add(offer.PackageVersion);
            if (MessageBox.Show(owner, SettingsText.Format("ExeUpdateOffer", offer.DisplayVersion), "WordSuggestor", MessageBoxButton.YesNo, MessageBoxImage.Information, MessageBoxResult.No) != MessageBoxResult.Yes) return;
            var launcher = await _service.DownloadVerifiedAsync(offer, token);
            token.ThrowIfCancellationRequested();
            if (_disposed) return;
            var ticket = new VerifiedUpdateHandoff(launcher, offer.PackageVersion, TimeProvider.System);
            new WindowsUpdateHandoff().CloseThenLaunch(owner, () =>
            {
                try { if (!ticket.TryOpen()) MessageBox.Show(SettingsText.Get("UpdateHandoffExpired"), "WordSuggestor"); }
                catch (Exception e) when (e is InvalidOperationException or System.ComponentModel.Win32Exception) { MessageBox.Show(SettingsText.Get("UpdateOpenFailed"), "WordSuggestor"); }
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception e) when (e is System.IO.IOException or InvalidOperationException or System.Net.Http.HttpRequestException or UnauthorizedAccessException)
        { if (!_disposed) MessageBox.Show(owner, SettingsText.Get("UpdateFlow.installer_unavailable"), "WordSuggestor", MessageBoxButton.OK, MessageBoxImage.Information); }
        finally { ExeUpdateInteraction.Exit(); }
    }
    public void Dispose()
    { if (_disposed) return; _disposed = true; _app.Activated -= Activated; _lifetime.Cancel(); _service.Dispose(); _lifetime.Dispose(); }
}
