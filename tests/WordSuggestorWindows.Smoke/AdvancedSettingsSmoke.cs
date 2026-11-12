using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Interop;
using WordSuggestorWindows.App;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

internal static class AdvancedSettingsSmoke
{
    private static string Repo() { for(var d=new DirectoryInfo(AppContext.BaseDirectory);d is not null;d=d.Parent) if(File.Exists(Path.Combine(d.FullName,"WordSuggestorWindows.sln")))return d.FullName;throw new Exception("No repository."); }
    private static string Contents(string root) => Directory.Exists(root) ? string.Join("\n",Directory.GetFiles(root,"runtime-v1.jsonl*").Select(File.ReadAllText)) : "";
    public static async Task Run(bool desktop)
    {
        var root=Path.Combine(Repo(),".artifacts","tests","advanced-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT",root);SettingsText.ApplyPreference("da-DK");var count=0;
        void Check(string title,bool value) { if(!value)throw new Exception("Advanced: "+title);count++;Console.WriteLine("PASS ADVANCED SETTINGS "+title); }
        var raw="{\"schemaVersion\":5,\"isAppDebugLoggingEnabled\":false,\"isCoreDebugLoggingEnabled\":true,\"shareWordChoices\":false,\"unknownConsent\":false}";
        var settingsPath=Path.Combine(root,"settings.json");File.WriteAllText(settingsPath,raw);var bytes=File.ReadAllBytes(settingsPath);var store=new WindowsAppSettingsService(settingsPath);var old=store.Load();
        Check("v5 migrates without new consent or diagnostic defaults",old.SchemaVersion==SettingsSchema.CurrentVersion && old.IsCoreDebugLoggingEnabled && !old.IsAppDebugLoggingEnabled && !old.IsPanelParityLoggingEnabled && !old.ShareWordChoices && !old.AdditionalProperties["unknownConsent"].GetBoolean());
        var changed=old.Clone();changed.IsPanelParityLoggingEnabled=true;store.SaveChanges(old,changed);
        Check("v5 exact backup and new field persist",File.ReadAllBytes(settingsPath+".v5.bak").SequenceEqual(bytes) && store.Load().IsPanelParityLoggingEnabled);
        var legacy=changed.Clone();var latest=changed.Clone();latest.IsPanelParityLoggingEnabled=false;store.Save(latest);legacy.IsAppDebugLoggingEnabled=true;store.SaveChanges(changed,legacy);
        Check("legacy merge preserves newer panel choice and unknown false consent",!store.Load().IsPanelParityLoggingEnabled && store.Load().IsAppDebugLoggingEnabled && !store.Load().AdditionalProperties["unknownConsent"].GetBoolean());
        var future=Path.Combine(root,"future.json");File.WriteAllText(future,"{\"schemaVersion\":99}");var futureStore=new WindowsAppSettingsService(future);var futureBytes=File.ReadAllBytes(future);var rejected=false;
        try{futureStore.Save(new());}catch(InvalidOperationException){rejected=true;}
        Check("future data cannot be overwritten by diagnostics",rejected && File.ReadAllBytes(future).SequenceEqual(futureBytes));

        var dir=Path.Combine(root,"logs");using var sink=new WindowsDiagnostics(dir);sink.Apply(new());
        foreach(var category in Enum.GetValues<DiagnosticCategory>())sink.Record(category,DiagnosticEvent.SettingsApplied);
        sink.RecordTiming(DiagnosticMetric.Suggestions,TimeSpan.FromMilliseconds(10));await sink.FlushAsync();
        Check("default off performs no file creation",!Directory.Exists(dir) && sink.Health==DiagnosticHealth.Disabled);
        var preferences=new AppSettingsSnapshot {IsAppDebugLoggingEnabled=true};sink.Apply(preferences);sink.Record(DiagnosticCategory.App,DiagnosticEvent.SettingsApplied);sink.Record(DiagnosticCategory.Placement,DiagnosticEvent.CaretFallback);await sink.FlushAsync();
        Check("App switch filters other categories",Contents(dir).Contains("SettingsApplied") && !Contents(dir).Contains("CaretFallback"));
        preferences.IsAppDebugLoggingEnabled=false;preferences.IsPanelParityLoggingEnabled=true;sink.Apply(preferences);sink.Record(DiagnosticCategory.Panel,DiagnosticEvent.OverlayExternal);sink.Record(DiagnosticCategory.App,DiagnosticEvent.CommitRejected);await sink.FlushAsync();
        Check("Panel category independent of App",Contents(dir).Contains("OverlayExternal") && !Contents(dir).Contains("CommitRejected"));
        preferences.IsPlacementDebugLoggingEnabled=true;sink.Apply(preferences);sink.Record(DiagnosticCategory.Placement,DiagnosticEvent.CaretFallback);await sink.FlushAsync();
        Check("Placement has its own effective category",Contents(dir).Contains("CaretFallback"));
        var before=Contents(dir);sink.Apply(new());sink.Record(DiagnosticCategory.App,DiagnosticEvent.LocalSession);sink.Record(DiagnosticCategory.Panel,DiagnosticEvent.OverlayLocal);await sink.FlushAsync();
        Check("disable stops subsequent collection and preserves existing files",Contents(dir)==before && sink.Health==DiagnosticHealth.Disabled);
        preferences=new(){IsPerformanceInstrumentationEnabled=true};sink.Apply(preferences);
        for(var i=0;i<3;i++)sink.RecordTiming(DiagnosticMetric.Suggestions,TimeSpan.FromMilliseconds(10));await sink.FlushAsync();
        var rows=Contents(dir).Split('\n',StringSplitOptions.RemoveEmptyEntries).Select(s=>JsonDocument.Parse(s)).ToArray();
        var aggregate=rows.Single(d=>d.RootElement.GetProperty("event").GetString()=="Aggregate").RootElement;
        Check("performance records aggregate counts total and max only",aggregate.GetProperty("count").GetInt64()==3 && aggregate.GetProperty("totalMicroseconds").GetInt64()==30000 && aggregate.GetProperty("maxMicroseconds").GetInt64()==10000);
        foreach(var row in rows)row.Dispose();
        var beforeReconfigure=Contents(dir);var staleMeasure=sink.Measure(DiagnosticMetric.CoreRoundtrip);sink.Apply(preferences);staleMeasure.Dispose();await sink.FlushAsync();
        Check("reconfiguration discards measurements started in an older epoch",Contents(dir)==beforeReconfigure);
        var deny=new CoreOperationalDiagnostics(1,"suggest","completed",1,41,0,0);sink.RecordCore(deny);await sink.FlushAsync();
        Check("untrusted Core counters rejected",!deny.ValidFor("suggest",false) && !Contents(dir).Contains("CoreSummary"));
        var unknownRejected=false;try{JsonSerializer.Deserialize<CoreOperationalDiagnostics>("{\"SchemaVersion\":1,\"Operation\":\"suggest\",\"Status\":\"completed\",\"Message\":\"PRIVATE_CANARY\"}");}catch(JsonException){unknownRejected=true;}
        Check("Core diagnostic DTO rejects free-form content",unknownRejected);
        var missingRejected=false;try{JsonSerializer.Deserialize<CoreOperationalDiagnostics>("{\"SchemaVersion\":1,\"Operation\":\"suggest\",\"Status\":\"completed\"}");}catch(JsonException){missingRejected=true;}
        Check("missing Core diagnostic fields cannot masquerade as zero counts",missingRejected);

        var fault=Path.Combine(root,"not-a-directory");File.WriteAllText(fault,"fixture");using(var failing=new WindowsDiagnostics(fault))
        {failing.Apply(new(){IsAppDebugLoggingEnabled=true});failing.Record(DiagnosticCategory.App,DiagnosticEvent.LocalSession);await failing.FlushAsync();Check("write error is bounded and does not throw into app",failing.Health==DiagnosticHealth.WriteUnavailable);}
        var rotate=Path.Combine(root,"rotation");Directory.CreateDirectory(rotate);var padding=string.Concat(Enumerable.Repeat("{\"fixture\":true}\n",WindowsDiagnostics.MaximumFileBytes/17));
        foreach(var suffix in new[]{"",".1",".2"})File.WriteAllText(Path.Combine(rotate,"runtime-v1.jsonl"+suffix),padding);
        using(var rotating=new WindowsDiagnostics(rotate))
        {
            rotating.Apply(new(){IsAppDebugLoggingEnabled=true});rotating.Record(DiagnosticCategory.App,DiagnosticEvent.LocalSession);await rotating.FlushAsync();
            Check("rotation bounded to three fixed files",Directory.GetFiles(rotate).Length==3 && Directory.GetFiles(rotate).All(p=>new FileInfo(p).Length<=WindowsDiagnostics.MaximumFileBytes) && File.ReadAllText(Path.Combine(rotate,"runtime-v1.jsonl")).Contains("LocalSession"));
            foreach(var file in Directory.GetFiles(rotate))File.SetLastWriteTimeUtc(file,DateTime.UtcNow.AddDays(-8));
            rotating.Record(DiagnosticCategory.App,DiagnosticEvent.ExternalSession);await rotating.FlushAsync();Check("lazy seven-day retention touches only owned files",Directory.GetFiles(rotate).Length==1);
            for(var i=0;i<10000;i++)rotating.Record(DiagnosticCategory.App,DiagnosticEvent.LocalSession);await rotating.FlushAsync();Check("burst bounded and drops counted",rotating.DroppedCount>0 && Directory.GetFiles(rotate).Length<=3);
        }

        var provider=new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root){Diagnostics=sink};sink.Apply(new(){IsCoreDebugLoggingEnabled=true});
        var caps=await provider.GetCapabilitiesAsync();Check("real Core advertises safe diagnostics with explicit import only",caps.OperationalDiagnostics && caps.UnknownWordLookup && caps.ExplicitUnknownWordImport && !caps.AutomaticUnknownWordObservation && !caps.UnknownWordSharing);
        await provider.SuggestWithOptionsAsync("skri",new(Guid.NewGuid(),1),new());await sink.FlushAsync();
        Check("real Core produces structured operational summary",Contents(dir).Contains("CoreSummary") && Contents(dir).Contains("Suggest"));
        var failedProvider=new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root,cliPath:Environment.ProcessPath){Diagnostics=sink};
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_CORE_MODE","exit");
        try{try{await failedProvider.SuggestAsync("PRIVATE_INPUT_CANARY_æøå",default);}catch(InvalidOperationException){ }}finally{Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_CORE_MODE",null);}
        await sink.FlushAsync();Check("failed process never leaks stdin stderr words paths or identifiers",Contents(dir).Contains("CoreFailed") && !Contents(dir).Contains("PRIVATE_") && !Contents(dir).Contains(root,StringComparison.OrdinalIgnoreCase));
        var unavailable=new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root,cliPath:Path.Combine(root,"missing.exe")){Diagnostics=sink};
        try{await unavailable.SuggestAsync("PRIVATE_MISSING_CORE",default);}catch(FileNotFoundException){ }
        await sink.FlushAsync();Check("missing Core has bounded status without path or text",Contents(dir).Contains("CoreUnavailable") && !Contents(dir).Contains("PRIVATE_") && !Contents(dir).Contains("missing.exe"));
        var safeFields=new[]{"schemaVersion","timestamp","category","event","operation","metric","count","totalMicroseconds","maxMicroseconds","candidateCount","diagnosticCount","documentWordCount"};
        Check("all diagnostic rows have only the closed metadata schema",Contents(dir).Split('\n',StringSplitOptions.RemoveEmptyEntries).All(s=>{using var d=JsonDocument.Parse(s);return d.RootElement.EnumerateObject().All(p=>safeFields.Contains(p.Name));}));
        sink.Apply(new());var completed=sink.Completion;sink.Dispose();await completed.WaitAsync(TimeSpan.FromSeconds(5));Check("view teardown drains without active capture",completed.IsCompletedSuccessfully);
        if(desktop)await Desktop(root,Check);
        Console.WriteLine($"PASS ADVANCED SETTINGS {count} checks; desktop={desktop}; explicit unknown-word import available; sharing unavailable; Hello deferred.");
    }
    private static async Task Desktop(string root,Action<string,bool> check)
    {
        var settings=new WindowsAppSettingsService(Path.Combine(root,"ui-settings.json"));settings.Save(new());using var logs=new WindowsDiagnostics(Path.Combine(root,"ui-logs"));
        using var vm=new MainWindowViewModel(new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root),new WindowsErrorInsightsStore(),settings,diagnostics:logs);
        var window=new SettingsWindow(vm);var foreground=GetForegroundWindow();GetCursorPos(out var cursor);
        T Find<T>(string name)where T:FrameworkElement=>(T)window.FindName(name);
        try
        {
            window.Show();window.Activate();((ListBox)window.FindName("CategoryNavigation")).SelectedIndex=7;await Task.Delay(150);
            var names=new[]{"PerformanceInstrumentationCheckBox","AppDebugLoggingCheckBox","PlacementDebugLoggingCheckBox","PanelParityLoggingCheckBox","CoreDebugLoggingCheckBox"};
            check("all five diagnostics are enabled and accessible",names.All(n=>Find<CheckBox>(n).IsEnabled && !string.IsNullOrWhiteSpace(UIElementAutomationPeer.CreatePeerForElement(Find<CheckBox>(n))!.GetName())));
            await Click(Find<CheckBox>(names[0]));await Until(()=>settings.Load().IsPerformanceInstrumentationEnabled);
            check("native mouse enables real performance authority",vm.Diagnostics.Enabled(DiagnosticCategory.Performance));
            Find<CheckBox>(names[1]).BringIntoView();Find<CheckBox>(names[1]).Focus();Key(new WindowInteropHelper(window).Handle,0x20);await Until(()=>settings.Load().IsAppDebugLoggingEnabled);
            check("native Space persists App and updates runtime",vm.Diagnostics.Enabled(DiagnosticCategory.App));
            foreach(var name in names.Skip(2))((IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(Find<CheckBox>(name))!.GetPattern(PatternInterface.Toggle)!).Toggle();
            await Until(()=>settings.Load().IsPlacementDebugLoggingEnabled && settings.Load().IsPanelParityLoggingEnabled && settings.Load().IsCoreDebugLoggingEnabled);
            check("UIA Toggle shares native persistence for independent categories",vm.Diagnostics.Enabled(DiagnosticCategory.Placement) && vm.Diagnostics.Enabled(DiagnosticCategory.Panel) && vm.Diagnostics.Enabled(DiagnosticCategory.Core));
            check("explicit lookup available off by default; sharing disabled",Find<CheckBox>("UnknownWordLookupCheckBox").IsEnabled && Find<CheckBox>("UnknownWordLookupCheckBox").IsChecked==false && !Find<CheckBox>("UnknownWordSharingCheckBox").IsEnabled && Find<CheckBox>("UnknownWordSharingCheckBox").IsChecked==false);
            var old=vm.LoadSettingsSnapshot();var draft=old.Clone();draft.IsPanelParityLoggingEnabled=false;vm.ApplySettingsSnapshot(draft);await Task.Delay(100);
            check("external stored change updates primary checkbox",Find<CheckBox>(names[3]).IsChecked==false);
            SettingsText.ApplyPreference("en-GB");check("diagnostic status and help translate without changing flags",Find<TextBlock>("AdvancedDiagnosticsStatus").Text==SettingsText.Get("AdvancedEnabled") && Find<CheckBox>(names[1]).IsChecked==true);SettingsText.ApplyPreference("da-DK");
            var legacyButton=Descendants(window).OfType<Button>().Single(b=>AutomationProperties.GetAutomationId(b)=="SettingsLegacyOpen");await Click(legacyButton);
            await Until(()=>Application.Current.Windows.OfType<LegacySettingsWindow>().Any());var legacy=Application.Current.Windows.OfType<LegacySettingsWindow>().Single();
            check("legacy opens separately with effective diagnostics",!ReferenceEquals(legacy,window) && ((CheckBox)legacy.FindName("AppDebugLoggingCheckBox")).IsEnabled);
            var primaryBase=vm.LoadSettingsSnapshot();var primaryEdit=primaryBase.Clone();primaryEdit.IsPanelParityLoggingEnabled=true;vm.ApplySettingsSnapshot(primaryEdit);
            ((CheckBox)legacy.FindName("AppDebugLoggingCheckBox")).IsChecked=false;
            await Click(Descendants(legacy).OfType<Button>().Single(b=>Equals(b.Content,SettingsText.Get("Save"))));
            check("legacy save cannot overwrite newer panel category",settings.Load().IsPanelParityLoggingEnabled && !settings.Load().IsAppDebugLoggingEnabled);
            check("reopening preserves independent diagnostic flags",new WindowsAppSettingsService(settings.SettingsPath).Load().IsPanelParityLoggingEnabled);
        }
        finally{foreach(var legacy in Application.Current.Windows.OfType<LegacySettingsWindow>().ToArray())legacy.Close();window.Close();SettingsText.ApplyPreference("da-DK");if(foreground!=IntPtr.Zero)SetForegroundWindow(foreground);SetCursorPos(cursor.X,cursor.Y);}
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root){for(var i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);i++){var child=System.Windows.Media.VisualTreeHelper.GetChild(root,i);yield return child;foreach(var d in Descendants(child))yield return d;}}
    private static async Task Until(Func<bool> condition){var t=Stopwatch.StartNew();while(!condition()){if(t.ElapsedMilliseconds>15000)throw new Exception("Advanced native/state wait timed out.");await Task.Delay(20);}}
    private static async Task Click(FrameworkElement target){target.BringIntoView();await Task.Delay(100);var handle=new WindowInteropHelper(Window.GetWindow(target)).Handle;await Until(()=>GetForegroundWindow()==handle);if(!target.IsVisible||!target.IsEnabled)throw new Exception("Advanced input unavailable.");var p=target.PointToScreen(new Point(target.ActualWidth/2,target.ActualHeight/2));SetCursorPos((int)p.X,(int)p.Y);if(GetForegroundWindow()!=handle)throw new Exception("Advanced input target changed.");mouse_event(2,0,0,0,UIntPtr.Zero);mouse_event(4,0,0,0,UIntPtr.Zero);await Task.Delay(100);}
    private static void Key(IntPtr target,byte key){if(GetForegroundWindow()!=target)throw new Exception("Advanced keyboard target changed.");keybd_event(key,0,0,UIntPtr.Zero);keybd_event(key,0,2,UIntPtr.Zero);}
    [StructLayout(LayoutKind.Sequential)]private struct Cursor{public int X,Y;}
    [DllImport("user32.dll")]private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]private static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")]private static extern bool GetCursorPos(out Cursor p);
    [DllImport("user32.dll")]private static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")]private static extern void mouse_event(uint f,uint x,uint y,uint d,UIntPtr e);
    [DllImport("user32.dll")]private static extern void keybd_event(byte k,byte s,uint f,UIntPtr e);
}
