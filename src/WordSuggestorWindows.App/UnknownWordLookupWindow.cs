using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

// Explicit import only. No passive typing/clipboard capture, no document insertion.
public sealed class UnknownWordLookupWindow : Window
{
    private readonly MainWindowViewModel _vm;
    private readonly LearnedWordStore _store;
    private readonly UnknownWordImportService? _service;
    private readonly Func<bool> _confirm;
    private readonly TextBox _word = new() { MaxLength = 128, Margin = new Thickness(0,8,0,8) };
    private readonly CheckBox _consent = new();
    private readonly TextBlock _status = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,12,0,8) };
    private readonly Button _lookup = new(), _cancel = new(), _clear = new();
    private CancellationTokenSource? _pending;
    private long _generation;
    private bool _busy, _closed, _loading;
    private long _lastAttempt;

    public UnknownWordLookupWindow(MainWindowViewModel vm, string initialWord = "", ICoreUnknownWordAdapter? core = null, LearnedWordStore? store = null, Func<bool>? confirm = null)
    {
        _vm=vm; _store=store??new(); _confirm=confirm??(()=>UnknownWordConsent.Request(this));
        if ((core??vm.UnknownWordCore) is { } adapter) _service=new(adapter,vm.SettingsService,_store);
        Title=SettingsText.Get("UnknownOpen"); Width=530; SizeToContent=SizeToContent.Height; MinHeight=280; ResizeMode=ResizeMode.NoResize;
        WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var panel=new StackPanel {Margin=new Thickness(20)}; Content=panel;
        panel.Children.Add(new TextBlock {Text=SettingsText.Get("UnknownExplicitOnly"),TextWrapping=TextWrapping.Wrap});
        panel.Children.Add(_word); _word.Text=initialWord.Length<=128 && !initialWord.Any(c=>char.IsWhiteSpace(c)||char.IsControl(c)) ? initialWord : "";
        _consent.Content=SettingsText.Get("UnknownAllow"); panel.Children.Add(_consent);
        _lookup.Content=SettingsText.Get("UnknownLookup"); _cancel.Content=SettingsText.Get("Cancel"); _clear.Content=SettingsText.Get("UnknownClear");
        var actions=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,12,0,0)};
        foreach(var button in new[]{_lookup,_cancel,_clear}) {button.Margin=new Thickness(0,0,8,0);button.Padding=new Thickness(10,5,10,5);actions.Children.Add(button);}
        panel.Children.Add(actions); panel.Children.Add(_status);
        SetAutomation(_word,"UnknownWordInput",SettingsText.Get("UnknownWord")); SetAutomation(_consent,"UnknownWordConsent",SettingsText.Get("UnknownAllow"));
        SetAutomation(_lookup,"UnknownWordLookup",SettingsText.Get("UnknownLookup")); SetAutomation(_cancel,"UnknownWordCancel",SettingsText.Get("Cancel"));
        SetAutomation(_clear,"UnknownWordClear",SettingsText.Get("UnknownClear")); SetAutomation(_status,"UnknownWordStatus",SettingsText.Get("UnknownStatus"));
        AutomationProperties.SetLiveSetting(_status,AutomationLiveSetting.Polite);
        _word.TextChanged+=(_,_)=>{_generation++;_pending?.Cancel();};
        _consent.Checked+=ConsentChanged; _consent.Unchecked+=ConsentChanged;
        _lookup.Click+=async(_,_)=>await Lookup(); _cancel.Click+=(_,_)=>_pending?.Cancel();
        _clear.Click+=async(_,_)=>await Clear();
        vm.SettingsChanged+=SettingsChanged;
        Deactivated+=(_,_)=>{_generation++;_pending?.Cancel();};
        Closed+=(_,_)=>{_closed=true;_pending?.Cancel();vm.SettingsChanged-=SettingsChanged;};
        Loaded+=(_,_)=>_word.Focus(); Refresh();
    }
    private static void SetAutomation(DependencyObject control,string id,string name) {AutomationProperties.SetAutomationId(control,id);AutomationProperties.SetName(control,name);}
    private void SettingsChanged(object? sender,AppSettingsSnapshot settings)
    { _generation++;_pending?.Cancel();Refresh(); }
    private void Refresh()
    {
        _loading=true;try{_consent.IsChecked=_vm.LoadSettingsSnapshot().IsUnknownWordLookupEnabled;}finally{_loading=false;}
        _consent.IsEnabled=_vm.CanWriteSettings;
        _lookup.IsEnabled=!_busy&&_service is not null;_word.IsEnabled=!_busy;_clear.IsEnabled=!_busy;_cancel.IsEnabled=_busy;
    }
    private void ConsentChanged(object sender,RoutedEventArgs e)
    {
        if(_loading)return;
        var before=_vm.LoadSettingsSnapshot();var edited=before.Clone();
        if(_consent.IsChecked==true&&!_confirm()){Refresh();return;}
        edited.IsUnknownWordLookupEnabled=_consent.IsChecked==true;edited.UnknownWordLookupConsentVersion=edited.IsUnknownWordLookupEnabled?1:0;
        if(!_vm.TryApplySettingsChanges(before,edited)) Set("UnknownSaveFailed");
        Refresh();
    }
    private void Set(string key){if(!_closed)_status.Text=SettingsText.Get(key);}
    private async Task Lookup()
    {
        if(_busy||_service is null)return;
        if(_vm.LoadSettingsSnapshot() is not {IsUnknownWordLookupEnabled:true,UnknownWordLookupConsentVersion:1}){Set("UnknownConsentRequired");return;}
        if(Environment.TickCount64-_lastAttempt<1000){Set("UnknownWait");return;}
        _lastAttempt=Environment.TickCount64;
        _pending?.Dispose();_pending=new();var token=_pending.Token;
        var generation=_generation;var word=_word.Text;var language=_vm.SelectedLanguageOption.LanguageCode;
        var scope=new CoreRequestContext(Guid.NewGuid(),generation);
        bool Current()=>!_closed&&!token.IsCancellationRequested&&generation==_generation&&IsActive&&_vm.SelectedLanguageOption.LanguageCode==language;
        _busy=true;Refresh();Set("UnknownWorking");
        try
        {
            var result=await _service.ImportAsync(word,scope,Current,token);
            Set(result switch {"saved"=>"UnknownSaved","not_candidate"=>"UnknownKnown","not_eligible"=>"UnknownNotEligible","not_found"=>"UnknownNotFound","consent_required"=>"UnknownConsentRequired",_=>"UnknownUnavailable"});
            if(result=="saved")_vm.RefreshSuggestionsPreview();
        }
        catch(OperationCanceledException){Set("UnknownCancelled");}
        catch(Exception ex)when(ex is IOException or InvalidOperationException or ArgumentException or TimeoutException or UnauthorizedAccessException or NotSupportedException or System.Text.Json.JsonException){Set("UnknownUnavailable");}
        finally{_busy=false;if(!_closed)Refresh();}
    }
    private async Task Clear()
    {
        if(_busy||MessageBox.Show(this,SettingsText.Get("UnknownClearConfirm"),"WordSuggestor",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.No)!=MessageBoxResult.Yes)return;
        _busy=true;Refresh();
        try{await _store.ClearAsync(CancellationToken.None);Set("UnknownCleared");_vm.RefreshSuggestionsPreview();}
        catch(Exception ex)when(ex is IOException or InvalidOperationException or UnauthorizedAccessException or System.Text.Json.JsonException){Set("UnknownUnavailable");}
        finally{_busy=false;if(!_closed)Refresh();}
    }
}
