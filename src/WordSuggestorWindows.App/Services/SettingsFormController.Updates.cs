using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace WordSuggestorWindows.App.Services;

internal sealed partial class SettingsFormController
{
    private bool _openingInstaller;
    private bool _renderingUpdate;
    private string? _updateUiStatusKey;
    private void AttachUpdateControls()
    {
        _updates.PropertyChanged += UpdateChanged;
        _unsubscribe.Add(() => _updates.PropertyChanged -= UpdateChanged);
        if (Optional<CheckBox>("AutomaticUpdatesCheckBox") is { } automatic)
        {
            RoutedEventHandler click = async (_, _) =>
            {
                if (_renderingUpdate || _disposed) return;
                var requested = automatic.IsChecked == true;
                if (_updates.IsDirectInstalled)
                {
                    _settings.CheckExeUpdatesAutomatically = requested;
                    CommitLive(); RenderUpdateState(); return;
                }
                RenderUpdateState(); // Never present a requested bool as actual OS state.
                await _updates.SetAutomaticAsync(requested);
            };
            automatic.Checked += click; automatic.Unchecked += click; automatic.Indeterminate += click;
            _unsubscribe.Add(() => { automatic.Checked -= click; automatic.Unchecked -= click; automatic.Indeterminate -= click; });
        }
        _host.Activated += UpdateHostActivated;
        _unsubscribe.Add(() => _host.Activated -= UpdateHostActivated);
        _ = _updates.RefreshAsync();
    }
    private void UpdateHostActivated(object? sender, EventArgs e)
    { if (!_disposed && !_openingInstaller) _ = _updates.RefreshAsync(); }
    private void UpdateChanged(object? sender, PropertyChangedEventArgs e)
    { if (!_disposed) RenderUpdateState(); }
    private void RenderUpdateState()
    {
        if (_disposed) return;
        Find<TextBlock>("VersionText").Text = _updates.VersionLabel;
        Find<Button>("CheckUpdatesButton").IsEnabled = !_updates.Busy && !_openingInstaller;
        var open = Find<Button>("OpenUpdateButton");
        open.Visibility = _updates.HasOffer ? Visibility.Visible : Visibility.Collapsed;
        open.IsEnabled = !_updates.Busy && !_openingInstaller;
        if (Optional<CheckBox>("AutomaticUpdatesCheckBox") is { } automatic)
        {
            _renderingUpdate = true;
            try { automatic.IsChecked = _updates.IsDirectInstalled ? (_updates.DirectChecksEnabled ? _settings.CheckExeUpdatesAutomatically : null) : _updates.PolicyState.Policy?.Automatic; }
            finally { _renderingUpdate = false; }
            automatic.IsEnabled = (_updates.IsDirectInstalled ? _updates.DirectChecksEnabled : _updates.PolicyState.CanChange) && !_updates.Busy && !_openingInstaller;
            var key = "UpdatePolicy." + _updates.PolicyState.Code;
            Find<TextBlock>("AutomaticUpdateStatus").Text = SettingsText.Get(SettingsText.Keys.Contains(key) ? key : "UpdatePolicy.os_unavailable");
        }
        var code = _updates.StatusCode;
        if (_updates.Busy && code == "idle") code = "checking";
        Find<TextBlock>("UpdateStatusText").Text = _updateUiStatusKey is { } status ? SettingsText.Get(status) : code == "idle" ? SettingsText.Get("UpdateIdle") :
            SettingsText.Format(SettingsText.Keys.Contains("UpdateFlow." + code) ? "UpdateFlow." + code : "UpdateFlow.os_unavailable", _updates.AvailableVersion);
    }
    public async void CheckUpdates_OnClick(object sender, RoutedEventArgs e)
    { if (!_openingInstaller) { _updateUiStatusKey = null; await _updates.CheckAsync(); } }
    public async void OpenUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        if (_openingInstaller || _updates.Busy || !_updates.HasOffer) return;
        var directLease = _updates.IsDirectInstalled;
        if (directLease && !ExeUpdateInteraction.TryEnter()) return;
        _openingInstaller = true; RenderUpdateState();
        try
        {
            if (MessageBox.Show(_host, SettingsText.Get(_updates.IsDirectInstalled ? "ExeUpdateConfirm" : "UpdateHandoffConfirm"), "WordSuggestor", MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
            var ticket = await _updates.PrepareHandoffAsync();
            if (_disposed || ticket is null) return;
            var closed = _updateHandoff.CloseThenLaunch(_host, () =>
            {
                try
                {
                    if (!ticket.TryOpen()) MessageBox.Show(SettingsText.Get("UpdateHandoffExpired"), "WordSuggestor", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception)
                { MessageBox.Show(SettingsText.Get("UpdateOpenFailed"), "WordSuggestor", MessageBoxButton.OK, MessageBoxImage.Information); }
            });
            if (!closed && !_disposed) _updateUiStatusKey = "UpdateHandoffCancelled";
        }
        finally { if (directLease) ExeUpdateInteraction.Exit(); _openingInstaller = false; if (!_disposed) RenderUpdateState(); }
    }
}
