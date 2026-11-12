using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using WordSuggestorWindows.App;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

internal static class UpdateSettingsSmoke
{
    private sealed class Clock : TimeProvider { public long Now; public override long TimestampFrequency=>1000; public override long GetTimestamp()=>Now; }
    private sealed class Launcher : IWindowsInstallerLauncher { public int Opened; public void Open()=>Opened++; }
    private sealed class Handoff : IWindowsUpdateHandoff { public int Calls; public bool Allow=true; public bool CloseThenLaunch(Window owner,Action open) { Calls++; if(Allow)open(); return Allow; } }
    private sealed class Handler : HttpMessageHandler
    {
        public Func<string> Body=()=>Feed("0.2.0.2"); public int Calls; public HttpStatusCode Status=HttpStatusCode.OK;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        { ct.ThrowIfCancellationRequested(); Calls++; return Task.FromResult(new HttpResponseMessage(Status){Content=new StringContent(Body())}); }
    }
    private sealed class Platform : IWindowsUpdatePlatform
    {
        public WindowsUpdatePolicy State=Policy(); public string Code="ready",Availability="available",SetCode="saved"; public bool Qualified=true,IgnoreWrite,ChangeOther; public int Writes,Checks; public TaskCompletionSource? Block;
        public async Task<WindowsUpdatePolicyResult> ReadAsync(CancellationToken ct) { if(Block is not null) await Block.Task; return new(Code,State,Qualified); }
        public Task<WindowsUpdateMutationResult> SetAsync(WindowsUpdatePolicy expected,bool enabled,CancellationToken ct)
        { Writes++; if(!IgnoreWrite)State=State with {OnLaunch=enabled,Background=enabled}; if(ChangeOther)State=State with {HoursBetweenChecks=1}; return Task.FromResult(new WindowsUpdateMutationResult(SetCode,State,false)); }
        public Task<NativeUpdateAvailability> CheckAsync(CancellationToken ct) { Checks++; return Task.FromResult(new NativeUpdateAvailability(Availability)); }
    }
    private static WindowsUpdatePolicy Policy()=>new("fixture-full","fixture-family",WindowsUpdateService.PackageName,"CN=Fixture",WindowsUpdateService.FeedUrl,new(0,2,0,1),false,false,6,true,false,false,true,"Default",[],[],[],[]);
    private static string Feed(string version)=>$"<AppInstaller xmlns=\"http://schemas.microsoft.com/appx/appinstaller/2018\" Version=\"{version}\" Uri=\"{WindowsUpdateService.FeedUrl}\"><MainPackage Name=\"{WindowsUpdateService.PackageName}\" Publisher=\"CN=Fixture\" Version=\"{version}\" ProcessorArchitecture=\"x64\" Uri=\"https://wordsuggestor.com/beta-artifacts/windows/x64/beta/WordSuggestor.Beta_{version}_x64.msix\"/></AppInstaller>";
    public static async Task Run(bool desktop)
    {
        SettingsText.ApplyPreference("da-DK"); var count=0;
        void Check(string label,bool value) { if(!value)throw new Exception("Update settings: "+label); count++; Console.WriteLine("PASS UPDATE SETTINGS "+label); }
        var handler=new Handler();var platform=new Platform();var launch=new Launcher();var clock=new Clock();
        var installed=new InstalledRelease(WindowsUpdateService.PackageName,"CN=Fixture",new(0,2,0,1));
        using var coordinator=new WindowsUpdateCoordinator(new(handler,installed),platform,launch,clock);
        await coordinator.RefreshAsync();
        Check("OS flags are read independently of app preferences",coordinator.PolicyState.Policy?.Automatic==false);
        Check("mixed actual schedules remain indeterminate",(platform.State with {Background=true}).Automatic is null);
        await coordinator.SetAutomaticAsync(true); Check("requested change needs matching re-read",coordinator.StatusCode=="policy_saved" && coordinator.PolicyState.Policy?.Automatic==true && platform.Writes==1);
        platform.IgnoreWrite=true; await coordinator.SetAutomaticAsync(false);
        Check("successful Set without actual OS effect is not success",coordinator.StatusCode=="policy_write_failed" && coordinator.PolicyState.Policy?.Automatic==true);
        platform.IgnoreWrite=false;platform.ChangeOther=true;await coordinator.SetAutomaticAsync(false);
        Check("unrelated policy change is rejected",coordinator.StatusCode=="policy_write_failed"); platform.ChangeOther=false;platform.State=Policy();
        platform.Qualified=false;await coordinator.RefreshAsync();var writes=platform.Writes;await coordinator.SetAutomaticAsync(true);
        Check("unqualified control performs no OS mutation",platform.Writes==writes && coordinator.StatusCode=="policy_readonly"); platform.Qualified=true;await coordinator.RefreshAsync();
        await coordinator.CheckAsync();Check("metadata and native availability both authorize offer",coordinator.HasOffer && coordinator.AvailableVersion==new Version(0,2,0,2) && launch.Opened==0);
        platform.Availability="current";await coordinator.CheckAsync();Check("feed alone does not offer installation",!coordinator.HasOffer && coordinator.StatusCode=="pending_os");
        platform.Availability="available";handler.Body=()=>Feed("0.2.0.1");await coordinator.CheckAsync();Check("native availability alone does not offer installation",!coordinator.HasOffer && coordinator.StatusCode=="pending_os");
        handler.Body=()=>Feed("0.2.0.2");platform.Availability="wrong_association";await coordinator.CheckAsync();Check("wrong OS association blocks handoff",!coordinator.HasOffer && await coordinator.PrepareHandoffAsync() is null);
        platform.Availability="available";await coordinator.CheckAsync();handler.Body=()=>Feed("0.2.0.2").Replace("CN=Fixture","CN=Other");
        Check("feed is revalidated before launch",await coordinator.PrepareHandoffAsync() is null && !coordinator.HasOffer && launch.Opened==0);
        handler.Body=()=>Feed("0.2.0.2");var ticket=await coordinator.PrepareHandoffAsync();
        Check("validated ticket does not launch automatically",ticket is not null && launch.Opened==0);
        Check("handoff ticket is one shot",ticket!.TryOpen() && !ticket.TryOpen() && launch.Opened==1);
        var expired=await coordinator.PrepareHandoffAsync();clock.Now+=120001;Check("monotonic expired offer cannot launch",!expired!.TryOpen() && launch.Opened==1);
        foreach(var (label,xml) in new[]{("forced downgrade",Feed("0.2.0.2").Replace("</AppInstaller>","<UpdateSettings><ForceUpdateFromAnyVersion>true</ForceUpdateFromAnyVersion></UpdateSettings></AppInstaller>")),("blocking activation",Feed("0.2.0.2").Replace("</AppInstaller>","<UpdateSettings><OnLaunch UpdateBlocksActivation=\"true\"/></UpdateSettings></AppInstaller>")),("unreviewed related package",Feed("0.2.0.2").Replace("</AppInstaller>","<OptionalPackages/></AppInstaller>")),("noncanonical version",Feed("0.2.0.2").Replace("0.2.0.2","00.2.0.2"))})
        { handler.Body=()=>xml;await coordinator.CheckAsync();Check(label+" feed rejected",coordinator.StatusCode=="invalid_feed" && !coordinator.HasOffer); }
        foreach(var xml in new[]{Feed("0.2.0.2").Replace("</AppInstaller>","<UpdateSettings><OnLaunch UpdateBlocksActivation=\"1\"/></UpdateSettings></AppInstaller>"),Feed("0.2.0.2").Replace("</AppInstaller>","<UpdateSettings><OnLaunch/><OnLaunch/></UpdateSettings></AppInstaller>")})
        { handler.Body=()=>xml;await coordinator.CheckAsync();Check("unsafe numeric/duplicate policy rejected",coordinator.StatusCode=="invalid_feed" && !coordinator.HasOffer); }
        var blocking=new Platform {Block=new()};
        using(var late=new WindowsUpdateCoordinator(new(new Handler(),installed),blocking,new Launcher()))
        {
            var work=late.RefreshAsync();Check("single flight blocks concurrent operations",!await late.CheckAsync());late.Dispose();blocking.Block!.SetResult();
            Check("closed view rejects late OS state",!await work && late.PolicyState.Policy is null);
        }
        var native=new WindowsNativeUpdatePlatform();var nativeState=await native.ReadAsync(default);
        Check("real loose process has no fabricated installed identity",WindowsUpdateService.ReadInstalledRelease() is null && nativeState.Code=="not_installed" && nativeState.Policy is null && !nativeState.CanChange);
        var denied=await native.SetAsync(Policy(),true,default);
        Check("production write gate cannot be enabled by a fake snapshot",denied.Code=="qualification_required");
        Check("production qualification arguments rejected without touching package",await WindowsUpdateQualification.TryRunAsync(["--qualify-update-policy",Guid.NewGuid().ToString("N")])==3);
        Check("activation helper rejects production target",UpdatePackageActivation.Start("WordSuggestor.Windows.Beta_fixture!App",Guid.NewGuid().ToString("N"))==2);
        Console.WriteLine("EVIDENCE native update write qualification remains BLOCKED; tests use a simulated policy adapter, not native authority. Hello gate deferred by user.");
        if(desktop) await Desktop(Check);
        Console.WriteLine($"PASS UPDATE SETTINGS {count} checks; desktop={desktop}.");
    }
    private static async Task Desktop(Action<string,bool> check)
    {
        var root=Path.Combine(FindRepo(),".artifacts","tests","update-ui-"+Guid.NewGuid().ToString("N")); Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT",root);
        var settings=new WindowsAppSettingsService();settings.Save(new AppSettingsSnapshot {InterfaceLocalePreference="da-DK"});var bytes=File.ReadAllBytes(settings.SettingsPath);
        using var vm=new MainWindowViewModel(new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root),new WindowsErrorInsightsStore(),settings);
        var platform=new Platform();var handler=new Handler();var launcher=new Launcher();var handoff=new Handoff();
        using var updates=new WindowsUpdateCoordinator(new(handler,new(WindowsUpdateService.PackageName,"CN=Fixture",new(0,2,0,1))),platform,launcher);
        var window=new SettingsWindow(vm,updates,handoff);var foreground=GetForegroundWindow();GetCursorPos(out var cursor);
        T Find<T>(string name) where T:FrameworkElement=>(T)window.FindName(name);
        try
        {
            window.Show();window.Activate();((ListBox)window.FindName("CategoryNavigation")).SelectedIndex=1;await Until(()=>!updates.Busy);
            var automatic=Find<CheckBox>("AutomaticUpdatesCheckBox");automatic.BringIntoView();await Task.Delay(100);await Click(automatic);await Until(()=>platform.Writes>=1 && !updates.Busy);
            check("native mouse policy choice is actual readback",automatic.IsChecked==true && platform.Writes==1);
            automatic.Focus();Chord(new WindowInteropHelper(window).Handle,0x20);await Until(()=>platform.Writes>=2 && !updates.Busy);
            check("native keyboard shares policy commit path",automatic.IsChecked==false && platform.Writes==2);
            ((IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(automatic)!.GetPattern(PatternInterface.Toggle)!).Toggle();await Until(()=>!updates.Busy);
            check("accessibility Toggle shares verified policy path",automatic.IsChecked==true && platform.Writes==3);
            check("no automatic-update bool persisted in user settings",bytes.SequenceEqual(File.ReadAllBytes(settings.SettingsPath)));
            await Click(Find<Button>("CheckUpdatesButton"));await Until(()=>!updates.Busy && updates.HasOffer);
            check("native manual check exposes separate installer action",Find<Button>("OpenUpdateButton").IsVisible && launcher.Opened==0);
            Find<Button>("OpenUpdateButton").BringIntoView();await Task.Delay(100);
            using(var reply=new DialogReply(false))await Click(Find<Button>("OpenUpdateButton"));
            check("native handoff cancellation does not close or launch",handoff.Calls==0 && launcher.Opened==0 && window.IsVisible);
            handoff.Allow=false;
            using(var reply=new DialogReply(true))await Click(Find<Button>("OpenUpdateButton"));await Task.Delay(100);
            check("cancelled save/close path preserves app and blocks launcher",handoff.Calls==1 && launcher.Opened==0 && Find<TextBlock>("UpdateStatusText").Text==SettingsText.Get("UpdateHandoffCancelled"));
            handoff.Allow=true;
            using(var reply=new DialogReply(true))await Click(Find<Button>("OpenUpdateButton"));await Task.Delay(100);
            check("explicit native confirmation revalidates before one launch",handoff.Calls==2 && launcher.Opened==1);
            SettingsText.ApplyPreference("en-GB");check("OS policy description follows interface language",Find<TextBlock>("AutomaticUpdateStatus").Text==SettingsText.Get("UpdatePolicy.ready") && platform.Writes==3);
        }
        finally {window.Close();SettingsText.ApplyPreference("da-DK");if(foreground!=IntPtr.Zero)SetForegroundWindow(foreground);SetCursorPos(cursor.X,cursor.Y);}
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source=new Uri("/WordSuggestorWindows.App;component/SharedStyles.xaml",UriKind.Relative) });
        using var vm2=new MainWindowViewModel(new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root),new WindowsErrorInsightsStore(),settings);
        var main=new MainWindow(vm2);var owned=new Window();var launched=0;
        try
        {
            main.Show();owned.Owner=main;vm2.EditorText="synthetic unsaved document";
            bool closed;using(var reply=new DialogReply(null))closed=new WindowsUpdateHandoff().CloseThenLaunch(owned,()=>launched++);
            check("real dirty-document Cancel prevents installer handoff",!closed && main.IsVisible && launched==0 && vm2.EditorText=="synthetic unsaved document");
            vm2.EditorText="";closed=new WindowsUpdateHandoff().CloseThenLaunch(owned,()=>launched++);
            check("real normal application close precedes launch",closed && launched==1 && !main.IsVisible);
        }
        finally {vm2.EditorText="";if(main.IsVisible)main.Close();if(foreground!=IntPtr.Zero)SetForegroundWindow(foreground);SetCursorPos(cursor.X,cursor.Y);}
    }
    private static string FindRepo(){for(var d=new DirectoryInfo(AppContext.BaseDirectory);d is not null;d=d.Parent)if(File.Exists(Path.Combine(d.FullName,"WordSuggestorWindows.sln")))return d.FullName;throw new Exception("No repo.");}
    private sealed class DialogReply:IDisposable
    {
        private readonly DispatcherTimer _timer=new(){Interval=TimeSpan.FromMilliseconds(50)};
        public DialogReply(bool? yes){_timer.Tick+=(_,_)=>{var h=GetForegroundWindow();GetWindowThreadProcessId(h,out var pid);var cls=new StringBuilder(64);GetClassName(h,cls,64);if(pid==Environment.ProcessId&&cls.ToString()=="#32770"){PostMessage(h,0x111,new IntPtr(yes is null?2:yes.Value?6:7),IntPtr.Zero);_timer.Stop();}};_timer.Start();}
        public void Dispose()=>_timer.Stop();
    }
    private static async Task Until(Func<bool> c){var sw=Stopwatch.StartNew();while(!c()){if(sw.ElapsedMilliseconds>15000)throw new Exception("Update UI timed out.");await Task.Delay(20);}}
    private static async Task Click(FrameworkElement e){var target=new WindowInteropHelper(Window.GetWindow(e)).Handle;await Until(()=>GetForegroundWindow()==target);if(!e.IsVisible||!e.IsEnabled)throw new Exception("Update input target unavailable.");var p=e.PointToScreen(new Point(e.ActualWidth/2,e.ActualHeight/2));SetCursorPos((int)p.X,(int)p.Y);if(GetForegroundWindow()!=target)throw new Exception("Update input target changed.");mouse_event(2,0,0,0,UIntPtr.Zero);mouse_event(4,0,0,0,UIntPtr.Zero);await Task.Delay(100);}
    private static void Chord(IntPtr target,params byte[] keys){if(GetForegroundWindow()!=target)throw new Exception("Input target changed.");foreach(var key in keys)keybd_event(key,0,0,UIntPtr.Zero);foreach(var key in keys.Reverse())keybd_event(key,0,2,UIntPtr.Zero);}
    [StructLayout(LayoutKind.Sequential)]private struct Cursor{public int X,Y;}
    [DllImport("user32.dll")]private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]private static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")]private static extern bool GetCursorPos(out Cursor p);
    [DllImport("user32.dll")]private static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")]private static extern void mouse_event(uint f,uint x,uint y,uint d,UIntPtr e);
    [DllImport("user32.dll")]private static extern void keybd_event(byte k,byte s,uint f,UIntPtr e);
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetClassName(IntPtr h,StringBuilder b,int n);
    [DllImport("user32.dll")]private static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
}
