using System.Windows;
using System.Windows.Controls;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

internal sealed partial class SettingsFormController
{
    private long _advancedCapabilityRevision;
    private string _advancedCoreStatus = "AdvancedCoreLoading";
    private bool _unknownWordAvailable;
    private bool _automaticWordAvailable;
    private void AttachAdvancedControls()
    {
        if (Optional<CheckBox>("UnknownWordLookupCheckBox") is { } lookup)
        {
            RoutedEventHandler changed = (_, _) =>
            {
                if (_loading || !_live || _disposed) return;
                if (lookup.IsChecked == true && !_confirmUnknownLookup())
                { _loading = true; try { lookup.IsChecked = false; } finally { _loading = false; } return; }
                CommitLive();
            };
            lookup.Checked += changed; lookup.Unchecked += changed;
            _unsubscribe.Add(() => { lookup.Checked -= changed; lookup.Unchecked -= changed; });
        }
        if(Optional<CheckBox>("AutomaticUnknownWordCheckBox") is {} automatic)
        {
            RoutedEventHandler changed=(_,_)=>{
                if(_loading||!_live||_disposed)return;
                if(automatic.IsChecked==true&&!_confirmAutomaticLookup())
                {_loading=true;try{automatic.IsChecked=false;}finally{_loading=false;}return;}
                CommitLive();
            };
            automatic.Checked+=changed;automatic.Unchecked+=changed;_unsubscribe.Add(()=>{automatic.Checked-=changed;automatic.Unchecked-=changed;});
        }
        if(Optional<CheckBox>("UnknownWordSharingCheckBox") is {} sharing)
        {
            RoutedEventHandler changed=(_,_)=>{
                if(_loading||!_live||_disposed)return;
                if(sharing.IsChecked==true&&(!UnknownWordSharingDeployment.Qualified||!_viewModel.AccountSession.IsSignedIn||!_viewModel.AccountSession.Config.Supports(UnknownWordSharingDeployment.Capability)||MessageBox.Show(_host,SettingsText.Get("UnknownSharingConsent"),"WordSuggestor",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)!=MessageBoxResult.Yes))
                {_loading=true;try{sharing.IsChecked=false;}finally{_loading=false;}return;}
                CommitLive();
            };
            sharing.Checked+=changed;sharing.Unchecked+=changed;_unsubscribe.Add(()=>{sharing.Checked-=changed;sharing.Unchecked-=changed;});
        }
        if (Optional<Button>("UnknownWordOpenButton") is { } open)
        {
            RoutedEventHandler click = (_, _) => new UnknownWordLookupWindow(_viewModel) { Owner = _host }.ShowDialog();
            open.Click += click; _unsubscribe.Add(() => open.Click -= click);
        }
        if(UnknownWordSharingDeployment.Qualified)
        {
            System.ComponentModel.PropertyChangedEventHandler accountChanged=(_,_)=>RenderAdvancedStatus();
            _viewModel.AccountSession.PropertyChanged+=accountChanged;_unsubscribe.Add(()=>_viewModel.AccountSession.PropertyChanged-=accountChanged);
        }
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
        if (Optional<CheckBox>("UnknownWordLookupCheckBox") is { } lookup) lookup.IsChecked = _settings.IsUnknownWordLookupEnabled;
        if(Optional<CheckBox>("UnknownWordSharingCheckBox") is {} sharing)sharing.IsChecked=_settings.IsUnknownWordCrowdHandoffEnabled;
        if(Optional<CheckBox>("AutomaticUnknownWordCheckBox") is {} automatic)automatic.IsChecked=_settings.IsAutomaticUnknownWordLookupEnabled;
        if (Optional<CheckBox>("PanelParityLoggingCheckBox") is { } check) check.IsChecked = _settings.IsPanelParityLoggingEnabled;
        RenderAdvancedStatus();
        if (Optional<TextBlock>("AdvancedCoreStatus") is not null) _ = RefreshAdvancedCapabilitiesAsync();
    }
    private void ReadAdvancedControls()
    {
        if (Optional<CheckBox>("UnknownWordLookupCheckBox") is { } lookup)
        { _settings.IsUnknownWordLookupEnabled = lookup.IsChecked == true; _settings.UnknownWordLookupConsentVersion = _settings.IsUnknownWordLookupEnabled ? 1 : 0; }
        if(Optional<CheckBox>("UnknownWordSharingCheckBox") is {} sharing){_settings.IsUnknownWordCrowdHandoffEnabled=sharing.IsChecked==true;_settings.UnknownWordSharingConsentVersion=_settings.IsUnknownWordCrowdHandoffEnabled?1:0;_settings.UnknownWordSharingConsentUserId=_settings.IsUnknownWordCrowdHandoffEnabled?_viewModel.AccountSession.Profile?.UserId??0:0;}
        if(Optional<CheckBox>("AutomaticUnknownWordCheckBox") is {} automatic){_settings.IsAutomaticUnknownWordLookupEnabled=automatic.IsChecked==true;_settings.UnknownWordAutomaticConsentVersion=_settings.IsAutomaticUnknownWordLookupEnabled?1:0;}
        if (Optional<CheckBox>("PanelParityLoggingCheckBox") is { } check) _settings.IsPanelParityLoggingEnabled = check.IsChecked == true;
    }
    private void RenderAdvancedStatus()
    {
        if (Optional<CheckBox>("UnknownWordLookupCheckBox") is { } lookup) lookup.IsEnabled = _unknownWordAvailable && _viewModel.CanWriteSettings;
        if(Optional<CheckBox>("UnknownWordSharingCheckBox") is {} sharing)sharing.IsEnabled=_unknownWordAvailable&&_viewModel.CanWriteSettings&&_settings.IsUnknownWordLookupEnabled&&UnknownWordSharingDeployment.Qualified&&_viewModel.AccountSession.IsSignedIn&&_viewModel.AccountSession.Config.Supports(UnknownWordSharingDeployment.Capability);
        if(Optional<CheckBox>("AutomaticUnknownWordCheckBox") is {} automatic)automatic.IsEnabled=_automaticWordAvailable&&_viewModel.CanWriteSettings&&_settings.IsUnknownWordLookupEnabled;
        if (Optional<Button>("UnknownWordOpenButton") is { } open) open.IsEnabled = _unknownWordAvailable;
        if (Optional<TextBlock>("UnknownWordSupportStatus") is { } support) support.Text = SettingsText.Get(_unknownWordAvailable ? "UnknownExplicitOnly" : "AdvancedUnknownUnavailable");
        if (Optional<TextBlock>("AdvancedCoreStatus") is { } core) core.Text = SettingsText.Get(_advancedCoreStatus);
        if (Optional<TextBlock>("AdvancedDiagnosticsStatus") is { } status) status.Text = SettingsText.Get(_viewModel.Diagnostics.Health switch
        { DiagnosticHealth.Disabled => "AdvancedDisabled", DiagnosticHealth.Ready => "AdvancedEnabled", _ => "AdvancedWriteUnavailable" });
    }
    private async Task RefreshAdvancedCapabilitiesAsync()
    {
        var revision = ++_advancedCapabilityRevision;
        var status = "AdvancedCoreTransportOnly";
        var available = false;var automaticAvailable=false;
        try
        {
            if (_viewModel.CoreSettings is { } adapter)
            {
                var caps = await adapter.GetCapabilitiesAsync(cancellationToken: _updateCancellation.Token);
                if (caps.OperationalDiagnostics) status = "AdvancedCoreStructured";
                automaticAvailable=caps.CompletedWordBoundaries&&caps.Supports("completedWord");
                available = caps.UnknownWordLookup && caps.ExplicitUnknownWordImport && caps.LearnedWordOverlay && caps.Supports("lookupUnknownWord") && caps.Supports("mergeLearnedWord");
            }
        }
        catch (OperationCanceledException) { return; }
        catch (Exception e) when (e is System.IO.IOException or InvalidOperationException or TimeoutException or UnauthorizedAccessException or NotSupportedException)
        { status = "AdvancedCoreUnavailable"; }
        if (_disposed || revision != _advancedCapabilityRevision) return;
        _automaticWordAvailable=automaticAvailable&&available;
        _unknownWordAvailable = available; _advancedCoreStatus = status; RenderAdvancedStatus();
    }
}
