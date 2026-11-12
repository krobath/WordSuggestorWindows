using System.Windows;
using System.Windows.Controls;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

internal sealed partial class SettingsFormController
{
    private long _advancedCapabilityRevision;
    private string _advancedCoreStatus = "AdvancedCoreLoading";
    private void AttachAdvancedControls()
    {
        _viewModel.Diagnostics.HealthChanged += DiagnosticHealthChanged;
        _unsubscribe.Add(() => _viewModel.Diagnostics.HealthChanged -= DiagnosticHealthChanged);
        if (Optional<CheckBox>("PanelParityLoggingCheckBox") is { } check)
        {
            RoutedEventHandler handler = (_, _) => CommitLive();
            check.Checked += handler; check.Unchecked += handler;
            _unsubscribe.Add(() => { check.Checked -= handler; check.Unchecked -= handler; });
        }
    }
    private void DiagnosticHealthChanged(object? sender, EventArgs e)
    {
        if (_disposed) return;
        try { _host.Dispatcher.BeginInvoke(new Action(() => { if (!_disposed) RenderAdvancedStatus(); })); }
        catch (InvalidOperationException) { /* Dispatcher shutdown must not fault the background writer. */ }
    }
    private void LoadAdvancedControls()
    {
        if (Optional<CheckBox>("PanelParityLoggingCheckBox") is { } check) check.IsChecked = _settings.IsPanelParityLoggingEnabled;
        RenderAdvancedStatus();
        if (Optional<TextBlock>("AdvancedCoreStatus") is not null) _ = RefreshAdvancedCapabilitiesAsync();
    }
    private void ReadAdvancedControls()
    {
        if (Optional<CheckBox>("PanelParityLoggingCheckBox") is { } check) _settings.IsPanelParityLoggingEnabled = check.IsChecked == true;
    }
    private void RenderAdvancedStatus()
    {
        if (Optional<TextBlock>("AdvancedCoreStatus") is { } core) core.Text = SettingsText.Get(_advancedCoreStatus);
        if (Optional<TextBlock>("AdvancedDiagnosticsStatus") is { } status) status.Text = SettingsText.Get(_viewModel.Diagnostics.Health switch
        { DiagnosticHealth.Disabled => "AdvancedDisabled", DiagnosticHealth.Ready => "AdvancedEnabled", _ => "AdvancedWriteUnavailable" });
    }
    private async Task RefreshAdvancedCapabilitiesAsync()
    {
        var revision = ++_advancedCapabilityRevision;
        var status = "AdvancedCoreTransportOnly";
        try
        {
            if (_viewModel.CoreSettings is { } adapter && (await adapter.GetCapabilitiesAsync(cancellationToken: _updateCancellation.Token)).OperationalDiagnostics) status = "AdvancedCoreStructured";
        }
        catch (OperationCanceledException) { return; }
        catch (Exception e) when (e is System.IO.IOException or InvalidOperationException or TimeoutException or UnauthorizedAccessException or NotSupportedException)
        { status = "AdvancedCoreUnavailable"; }
        if (_disposed || revision != _advancedCapabilityRevision) return;
        _advancedCoreStatus = status; RenderAdvancedStatus();
    }
}
