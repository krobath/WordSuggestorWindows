using System.IO;
using System.Diagnostics;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using WordSuggestorWindows.App;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

internal static class AutomaticWordSmoke
{
    const string Word="zzzultrakandidat";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    public static async Task Run(string repo)
    {
        var root=Path.Combine(repo,".artifacts","tests","automatic-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT",root);
        var manual=Path.Combine(root,"UserWords","local_lexicon_overlay_v2.json");Directory.CreateDirectory(Path.GetDirectoryName(manual)!);File.WriteAllText(manual,Empty);
        var settings=new WindowsAppSettingsService();settings.Save(new(){IsGlobalCaptureEnabled=false});
        var count=0;void Check(string title,bool ok){if(!ok)throw new Exception(title);count++;Console.WriteLine("PASS AUTOMATIC WORD "+title);}
        var scope=new CoreRequestContext(Guid.NewGuid(),1);var fake=new Fake();long clock=10000;
        using(var coordinator=new AutomaticWordLearning(fake,fake,settings,new(root),()=>clock))
        {
            Check("default never observes or transmits",await coordinator.ObserveAsync(Word+" ",scope,()=>true)=="disabled"&&fake.Calls==0);
            var s=settings.Load();s.IsUnknownWordLookupEnabled=true;s.UnknownWordLookupConsentVersion=1;settings.Save(s);
            Check("manual consent never enables automatic capture",await coordinator.ObserveAsync(Word+" ",scope,()=>true)=="disabled"&&fake.Calls==0);
            s.IsAutomaticUnknownWordLookupEnabled=true;s.UnknownWordAutomaticConsentVersion=1;settings.Save(s);
            Check("unverified target never contacts Core",await coordinator.ObserveAsync(Word+" ",scope,()=>false)=="disabled"&&fake.Calls==0);
            Check("separate consent enables validated import",await coordinator.ObserveAsync(Word+" ",scope,()=>true)=="saved");
            Check("throttle bounds dictionary calls",await coordinator.ObserveAsync(Word+" ",scope,()=>true)=="rate_limited");clock+=4000;
            Check("same word is deduplicated before provider",await coordinator.ObserveAsync(Word+" ",scope,()=>true)=="duplicate");
        }
        using(var rate=new AutomaticWordLearning(new Fake(),new Fake(),settings,new(root),()=>clock))
        {
            var outcomes=new List<string>();for(var i=0;i<11;i++){outcomes.Add(await rate.ObserveAsync(Word+i+" ",scope,()=>true));clock+=4000;}
            Check("minute budget permits ten attempts then stops",outcomes.Take(10).All(x=>x=="saved")&&outcomes[10]=="rate_limited");
        }
        var migrating=settings.Load();migrating.SchemaVersion=7;Check("v7 preserves manual consent but never creates automatic consent",SettingsSchema.Upgrade(migrating) is {IsUnknownWordLookupEnabled:true,IsAutomaticUnknownWordLookupEnabled:false,UnknownWordAutomaticConsentVersion:0});
        fake=new();var current=true;
        using(var pending=new AutomaticWordLearning(fake,fake,settings,new(root),()=>clock))
        {
            fake.AfterBoundary=()=>current=false;
            Check("stale target after Core boundary never reaches provider",await pending.ObserveAsync(Word+" ",scope,()=>current)=="cancelled"&&fake.Lookups==0);
            current=true;fake.AfterBoundary=null;fake.AfterLookup=()=>{var s=settings.Load();s.IsAutomaticUnknownWordLookupEnabled=false;settings.Save(s);};
            var before=File.ReadAllBytes(LearnedWordStore.PathFor(root));
            Check("revoked automatic consent prevents late persistence",await pending.ObserveAsync(Word+" ",scope,()=>true)=="cancelled"&&File.ReadAllBytes(LearnedWordStore.PathFor(root)).SequenceEqual(before));
        }
        await new LearnedWordStore(root).ClearAsync(default);
        var cli=Environment.GetEnvironmentVariable("WORDSUGGESTOR_SUGGEST_CLI_PATH")??Path.GetFullPath(Path.Combine(repo,"../WordSuggestorCore/.build/x86_64-unknown-windows-msvc/debug/WordSuggestorSuggestCLI.exe"));
        var fixture=Path.Combine(root,"provider.json");File.WriteAllText(fixture,JsonSerializer.Serialize(new[]{new{termNorm=Word,display=Word,gram="sb",lemmaNorm=Word,freq=10,source="fixture",sourceRecordID="fixture:1"},new{termNorm=Word+"to",display=Word+"to",gram="sb",lemmaNorm=Word+"to",freq=10,source="fixture",sourceRecordID="fixture:2"}}));
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_MODE","real");Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_CLI",cli);Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_UNKNOWN_FIXTURE",fixture);
        try
        {
            var provider=new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root,cliPath:Environment.ProcessPath);
            Check("real Core owns original token range",(await provider.CompletedWordAsync("x "+Word+" ",10,scope)).Value is {Start:2,Length:16});
            Check("known word and clipped fragment are not imported",(await provider.CompletedWordAsync("skrive ",0,scope)).Value.Start is null&&(await provider.CompletedWordAsync(Word+" ",10,scope)).Value.Start is null);
            var s=settings.Load();s.IsAutomaticUnknownWordLookupEnabled=true;s.UnknownWordAutomaticConsentVersion=1;settings.Save(s);
            Application.Current.Resources.MergedDictionaries.Add(new(){Source=new Uri("/WordSuggestorWindows.App;component/SharedStyles.xaml",UriKind.Relative)});
            using var vm=new MainWindowViewModel(provider,new(),settings);
            var noAutomatic=settings.Load();noAutomatic.IsAutomaticUnknownWordLookupEnabled=false;settings.Save(noAutomatic);
            var allow=false;var form=new SettingsWindow(vm,confirmUnknownLookup:()=>true,confirmAutomaticLookup:()=>allow);
            try{
                form.Show();form.Activate();((ListBox)form.FindName("CategoryNavigation")).SelectedIndex=7;await Task.Delay(250);
                var automatic=(CheckBox)form.FindName("AutomaticUnknownWordCheckBox");
                Check("automatic preference is accessible and enabled independently",automatic.IsEnabled&&!string.IsNullOrWhiteSpace(UIElementAutomationPeer.CreatePeerForElement(automatic)!.GetName()));
                void Toggle(CheckBox c)=>((IToggleProvider)UIElementAutomationPeer.CreatePeerForElement(c)!.GetPattern(PatternInterface.Toggle)!).Toggle();
                Toggle(automatic);Check("declined automatic consent stays off",!settings.Load().IsAutomaticUnknownWordLookupEnabled);
                allow=true;Toggle(automatic);Check("approved automatic consent persists separately",settings.Load() is {IsAutomaticUnknownWordLookupEnabled:true,UnknownWordAutomaticConsentVersion:1});
                Toggle((CheckBox)form.FindName("UnknownWordLookupCheckBox"));Check("revoking lookup also revokes automatic permission",settings.Load() is {IsAutomaticUnknownWordLookupEnabled:false,UnknownWordAutomaticConsentVersion:0});
                Toggle((CheckBox)form.FindName("UnknownWordLookupCheckBox"));Toggle(automatic);
            }finally{form.Close();}
            var main=new MainWindow(vm);var editor=(InternalEditorWindow)typeof(MainWindow).GetField("_editorWindow",Private)!.GetValue(main)!;
            IntPtr notepad=IntPtr.Zero;
            try
            {
                main.Show();vm.EnsureEditorExpanded();editor.ShowEditor();await Task.Delay(250);
                var handle=new System.Windows.Interop.WindowInteropHelper(editor).Handle;
                Check("native activating editor owns input",GetForegroundWindow()==handle);
                // Seed is programmatic and must never trigger lookup. Only the
                // subsequent physical boundary receives a verified input receipt.
                vm.EditorText=Word;vm.CaretIndex=Word.Length;editor.ShowEditor();await Task.Delay(250);
                Check("programmatic text import does not learn",!File.Exists(LearnedWordStore.PathFor(root)));
                Key(handle,0x20);
                var deadline=Environment.TickCount64+10000;while(!File.Exists(LearnedWordStore.PathFor(root))&&Environment.TickCount64<deadline)await Task.Delay(25);
                Console.WriteLine("AUTO UI state="+typeof(MainWindow).GetField("_unknownLastStatus",Private)!.GetValue(main)+" length="+vm.EditorText.Length+" active="+editor.IsActive);
                Check("physical editor boundary learns through real Core",File.Exists(LearnedWordStore.PathFor(root)));
                Check("learning never inserts or changes document text",vm.EditorText==Word+" "&&GetForegroundWindow()==handle);
                var external=settings.Load();external.IsGlobalCaptureEnabled=true;settings.Save(external);
                var file=Path.Combine(root,"auto-"+Guid.NewGuid().ToString("N")+".txt");File.WriteAllText(file,Word+"to");
                using var launcher=Process.Start(new ProcessStartInfo("notepad.exe"){UseShellExecute=true,ArgumentList={file}});
                deadline=Environment.TickCount64+10000;while((notepad=FindWindow(Path.GetFileNameWithoutExtension(file)))==IntPtr.Zero&&Environment.TickCount64<deadline)await Task.Delay(25);
                if(notepad==IntPtr.Zero)throw new Exception("Owned Notepad not found.");SetForegroundWindow(notepad);await Task.Delay(200);
                keybd_event(0x11,0,0,UIntPtr.Zero);try{Key(notepad,0x23);}finally{keybd_event(0x11,0,2,UIntPtr.Zero);}
                await Task.Delay(3500);
                var adapter=new WindowsNativeTextAdapter();Check("automatic external route verifies owned Notepad",adapter.ReadFocused() is {} before&&before.Window==notepad&&before.Start==18&&before.End==18);
                Key(notepad,0x20);
                deadline=Environment.TickCount64+10000;while(!File.ReadAllText(LearnedWordStore.PathFor(root)).Contains(Word+"to")&&Environment.TickCount64<deadline)await Task.Delay(25);
                Check("physical Notepad boundary learns through real Core",File.ReadAllText(LearnedWordStore.PathFor(root)).Contains(Word+"to"));
                Check("external learning preserves document and focus",adapter.ReadFocused() is {} after&&after.Text==Word+"to "&&GetForegroundWindow()==notepad);
                keybd_event(0x11,0,0,UIntPtr.Zero);try{Key(notepad,0x53);}finally{keybd_event(0x11,0,2,UIntPtr.Zero);}
                await Task.Delay(150);

            }
            finally
            {
                if(notepad!=IntPtr.Zero)PostMessage(notepad,0x10,IntPtr.Zero,IntPtr.Zero);
                vm.EditorText="";
                var workspace=(EditorWorkspace)typeof(MainWindow).GetField("_workspace",Private)!.GetValue(main)!;foreach(var tab in workspace.Tabs)tab.UpdateText("");main.Close();
            }
            Check("manual user words remain unchanged",File.ReadAllText(manual)==Empty);
        }
        finally{foreach(var name in new[]{"WORDSUGGESTOR_TEST_UNKNOWN_MODE","WORDSUGGESTOR_TEST_UNKNOWN_CLI","WORDSUGGESTOR_TEST_UNKNOWN_FIXTURE"})Environment.SetEnvironmentVariable(name,null);}
        Console.WriteLine($"PASS AUTOMATIC WORD {count} checks; real Core and physical editor input; sharing disabled.");
    }
    const string Empty="{\"schema_version\":\"local_lexicon_overlay_v2\",\"entries_by_language\":{}}";
    sealed class Fake:ICoreCompletedWordAdapter,ICoreUnknownWordAdapter
    {
        public int Calls,Lookups;public Action? AfterBoundary,AfterLookup;
        public Task<CoreQueryResult<CoreCompletedToken>> CompletedWordAsync(string prefix,int offset,CoreRequestContext scope,CancellationToken token=default){Calls++;AfterBoundary?.Invoke();return Task.FromResult(new CoreQueryResult<CoreCompletedToken>(scope,"da-DK",new(1,0,prefix.Length-1),null));}
        public Task<CoreQueryResult<CoreUnknownWordResult>> LookupUnknownWordAsync(string surface,CoreRequestContext scope,int consent,CancellationToken token=default){Lookups++;AfterLookup?.Invoke();return Task.FromResult(new CoreQueryResult<CoreUnknownWordResult>(scope,"da-DK",new(1,"resolved",JsonSerializer.SerializeToElement(new{display=surface}),null),null));}
        public Task<CoreQueryResult<CoreUnknownWordResult>> PrepareLearnedWordAsync(string surface,JsonElement proposal,CoreRequestContext scope,int consent,CancellationToken token=default)=>Task.FromResult(new CoreQueryResult<CoreUnknownWordResult>(scope,"da-DK",new(1,"prepared",null,Empty),null));
    }
    delegate bool WindowCallback(IntPtr handle,IntPtr arg);
    static IntPtr FindWindow(string title){var found=IntPtr.Zero;EnumWindows((h,_)=>{var text=new StringBuilder(512);GetWindowText(h,text,512);if(text.ToString().Contains(title))found=h;return true;},IntPtr.Zero);return found;}
    [DllImport("user32.dll")]static extern bool EnumWindows(WindowCallback call,IntPtr arg);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr h,StringBuilder text,int count);
    [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")]static extern bool PostMessage(IntPtr h,uint message,IntPtr first,IntPtr second);
    static void Key(IntPtr target,byte key){if(GetForegroundWindow()!=target)throw new Exception("Automatic test target changed.");keybd_event(key,0,0,UIntPtr.Zero);keybd_event(key,0,2,UIntPtr.Zero);}
    [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]static extern void keybd_event(byte key,byte scan,uint flags,UIntPtr extra);
}
