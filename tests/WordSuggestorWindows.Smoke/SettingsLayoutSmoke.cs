using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WordSuggestorWindows.App;
using WordSuggestorWindows.App.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

// Actual WPF layout/focus/peers; raster scales do not impersonate OS mixed-DPI tests.
internal static class SettingsLayoutSmoke
{
    public static async Task Run(string repo)
    {
        var root=Path.Combine(repo,".artifacts","tests","settings-layout-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var priorRoot=Environment.GetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT");
        var priorLocale=SettingsText.ResolvedLocale;
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT",root);
        var store=new WindowsAppSettingsService();
        store.Save(new AppSettingsSnapshot { IsGlobalCaptureEnabled=false });
        var provider=new WordSuggestorCoreCliSuggestionProvider(localDataRoot:root);
        using var vm=new MainWindowViewModel(provider,new WindowsErrorInsightsStore(),store,"");
        using var account=new AccountSettingsSmoke.Fixture();
        await account.Session.InitializeAsync();
        typeof(MainWindowViewModel).GetField("_accountSession",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(vm,account.Session);
        SettingsWindow? window=null;var count=0;
        void Check(string name,bool ok) { if(!ok)throw new InvalidOperationException("Settings layout: "+name);count++; }
        try
        {
            window=new SettingsWindow(vm);window.Show();window.Activate();
            var nav=(ListBox)window.FindName("CategoryNavigation");
            var scroll=(ScrollViewer)window.FindName("DetailScroll");
            foreach(var signedIn in new[]{false,true})
            {
            if(signedIn)Check("synthetic account login",await account.Session.LoginAsync("fixture@example.invalid","SyntheticPassword12"));
            foreach(var locale in new[]{"da-DK","en-GB"})
            {
                SettingsText.ApplyPreference(locale);
                foreach(var size in new[]{new Size(760,480),new Size(1000,700)})
                {
                    window.Width=size.Width;window.Height=size.Height;
                    for(var category=0;category<nav.Items.Count;category++)
                    {
                        nav.SelectedIndex=category;await Task.Delay(35);window.UpdateLayout();
                        var prefix=signedIn+"/"+locale+"/"+size.Width+"/"+category;
                        var item=(ListBoxItem)nav.Items[category];
                        Check(prefix+" category peer",!string.IsNullOrWhiteSpace(UIElementAutomationPeer.CreatePeerForElement(item)?.GetName()));
                        var rows=Children(window).OfType<SettingsRow>().Where(x=>x.IsVisible).ToArray();
                        Check(prefix+" rows",rows.Length>0);
                        Check(prefix+" no horizontal clipping",scroll.ExtentWidth<=scroll.ViewportWidth+1);
                        foreach(var row in rows)
                        {
                            var help=Children(row).OfType<Button>().Single(b=>AutomationProperties.GetAutomationId(b)=="SettingsHelp_"+row.ControlId);
                            var peer=UIElementAutomationPeer.CreatePeerForElement(help);
                            Check(prefix+" named help "+row.ControlId,peer is not null&&!string.IsNullOrWhiteSpace(peer.GetName())&&!string.IsNullOrWhiteSpace(peer.GetHelpText())&&help.IsTabStop);
                            if(row.Content is Control editor&&editor.IsEnabled)
                            {
                                Check(prefix+" named editor "+row.ControlId,!string.IsNullOrWhiteSpace(UIElementAutomationPeer.CreatePeerForElement(editor)?.GetName()));
                                if(row.ControlId is "G01" or "G02" or "O03")
                                    Check(prefix+" localized editor name "+row.ControlId,UIElementAutomationPeer.CreatePeerForElement(editor)?.GetName()==row.Header?.ToString());
                            }
                        }
                        var editorIds=rows.Where(r=>r.Content is Control {IsEnabled:true}).Select(r=>AutomationProperties.GetAutomationId((Control)r.Content)).ToArray();
                        var handle=new System.Windows.Interop.WindowInteropHelper(window).Handle;
                        Check(prefix+" native UIA editor names",await Task.Run(()=>
                        {
                            var element=AutomationElement.FromHandle(handle);
                            return editorIds.All(id=>!string.IsNullOrWhiteSpace(element.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,id))?.Current.Name));
                        }));
                        // Unavailable account sections intentionally disable their entire row.
                        // Reload stays available; exercise the last enabled content button.
                        var lastHelp=Children(scroll).OfType<Button>().Last(b=>b.IsEnabled&&b.IsVisible&&b.IsTabStop);
                        lastHelp.Focus();lastHelp.BringIntoView();await Task.Delay(25);window.UpdateLayout();
                        var bounds=lastHelp.TransformToAncestor(scroll).TransformBounds(new Rect(lastHelp.RenderSize));
                        Check(prefix+$" last help keyboard focus/scroll (enabled={lastHelp.IsEnabled}, focus={lastHelp.IsKeyboardFocused}, top={bounds.Top:0}, bottom={bounds.Bottom:0}, viewport={scroll.ActualHeight:0})",lastHelp.IsKeyboardFocused&&bounds.Top>=-1&&bounds.Bottom<=scroll.ActualHeight+1);
                        nav.Focus();Check(prefix+" rail focus",nav.IsKeyboardFocusWithin);
                    }
                }
            }
            }
            nav.SelectedIndex=3;scroll.ScrollToTop();await Task.Delay(35);window.UpdateLayout();
            var visual=(FrameworkElement)window.Content;
            foreach(var scale in new[]{1d,1.25,1.5,2})
            {
                var bitmap=new RenderTargetBitmap((int)Math.Ceiling(visual.ActualWidth*scale),(int)Math.Ceiling(visual.ActualHeight*scale),96*scale,96*scale,PixelFormats.Pbgra32);
                bitmap.Render(visual);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var file=File.Create(Path.Combine(root,"raster-"+(int)(scale*100)+".png"));encoder.Save(file);
                Check("raster scale "+scale,file.Length>1000);
            }
            var dpi=VisualTreeHelper.GetDpi(window);
            Console.WriteLine($"PASS SETTINGS LAYOUT {count} checks; 2 account states, 2 locales, 8 categories, minimum/default sizes, help/editor peers and focus; actual DPI {dpi.PixelsPerInchX:0}; raster scales 100/125/150/200 only.");
        }
        finally { window?.Close();SettingsText.ApplyPreference(priorLocale);Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT",priorRoot); }
    }
    private static IEnumerable<DependencyObject> Children(DependencyObject root)
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        { var child=VisualTreeHelper.GetChild(root,i);yield return child;foreach(var descendant in Children(child))yield return descendant; }
    }
}
