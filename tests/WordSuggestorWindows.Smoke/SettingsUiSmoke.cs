using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WordSuggestorWindows.App;
using WordSuggestorWindows.App.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

// Real WPF windows, native mouse/keyboard input, isolated user data. UIA is
// queried on a worker so cross-process automation cannot deadlock the UI thread.
internal static class SettingsUiSmoke
{
    public static async Task Run(string repo)
    {
        var root = Path.Combine(repo, ".artifacts", "tests", "settings-ui-" + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", root);
        var store = new WindowsAppSettingsService();
        var initial = new AppSettingsSnapshot();
        initial.AdditionalProperties["futureUiOption"] = JsonSerializer.SerializeToElement("preserve");
        initial.SystemVoiceIdOverrideByLanguage["da-dk"] = "not-installed-voice-canary";
        store.Save(initial);
        var provider = new WordSuggestorCoreCliSuggestionProvider(localDataRoot: root);
        using var vm = new MainWindowViewModel(provider, new WindowsErrorInsightsStore(), store, "");
        var checks = 0;
        void Check(string title, bool ok) { if (!ok) throw new Exception(title); checks++; Console.WriteLine("PASS SETTINGS UI " + title); }
        var originalForeground = GetForegroundWindow(); GetCursorPos(out var originalCursor);
        SettingsWindow? window = null;
        try
        {
            window = new SettingsWindow(vm); window.Show(); window.Activate(); await Pause();
            T Find<T>(string name) where T : FrameworkElement => (T)window.FindName(name);
            var nav = Find<ListBox>("CategoryNavigation");
            var ids = new[] { "profile", "general", "speech", "suggestions", "textAnalysis", "learning", "errorTracking", "advanced" };
            Check("eight categories in reference order", nav.Items.Cast<ListBoxItem>().Select(i => i.Tag).SequenceEqual(ids));
            Check("profile initially selected", nav.SelectedIndex == 0 && Find<StackPanel>("ProfilePage").IsVisible);
            Check("primary view has no horizontal tab bar", !Descendants(window).OfType<TabControl>().Any());
            var handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            var automation = await Task.Run(() =>
            {
                var target = AutomationElement.FromHandle(handle);
                return target.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ListItem))
                    .Cast<AutomationElement>().Select(e => (e.Current.AutomationId, e.Current.Name)).ToArray();
            });
            Check("category IDs and names exposed through UI Automation", automation.Length == 8 && automation.Select(a => a.AutomationId).SequenceEqual(ids.Select(id => "SettingsCategory_" + id)) && automation.All(a => a.Name.Length > 0));
            await Click((FrameworkElement)nav.Items[1]);
            Check("mouse switches to General", nav.SelectedIndex == 1 && Find<StackPanel>("GeneralPage").IsVisible);
            Key(0x28); await Pause();
            Check("arrow key selects next category", nav.SelectedIndex == 2 && Find<StackPanel>("SpeechPage").IsVisible);
            Key(0x26); await Pause();
            Check("arrow key returns and keeps rail focus", nav.SelectedIndex == 1 && nav.IsKeyboardFocusWithin);
            Check("version preserved", Find<TextBlock>("VersionText").Text.Contains("WordSuggestor"));
            Check("current version value readable through UI Automation", await Task.Run(() => AutomationElement.FromHandle(handle).FindFirst(TreeScope.Descendants,
                new PropertyCondition(AutomationElement.AutomationIdProperty, "AppVersion"))?.Current.Name.Contains("WordSuggestor") == true));
            Capture(window, Path.Combine(root, "general.png"));
            Check("automatic update control does not pretend OS authority", Descendants(Find<StackPanel>("GeneralPage")).OfType<SettingsRow>().Single(r => r.ControlId == "G05").Content is TextBlock);
            var helpA = Help(window, "G01"); var helpB = Help(window, "G02");
            Check("help has meaningful accessible name", AutomationProperties.GetName(helpA).Contains("Interface-sprog"));
            await Click(helpA);
            Check("help opens and receives keyboard focus", HelpOpen() && Keyboard.FocusedElement is Button b && AutomationProperties.GetAutomationId(b) == "SettingsHelpClose");
            Key(0x1B); await Pause();
            Check("Escape closes help and restores originating focus", !HelpOpen() && helpA.IsKeyboardFocused && window.IsVisible);
            await Click(helpA); await Click(helpB);
            Check("only one help popup is open", OpenHelpCount() == 1);
            await Click(helpB);
            Check("same help icon toggles closed", !HelpOpen());
            await Click(helpA); await Click((FrameworkElement)nav.Items[2]);
            Check("category change closes help and focuses navigation", !HelpOpen() && nav.IsKeyboardFocusWithin);
            Check("unimplemented reading strategy disabled", !Find<ComboBox>("ReadingStrategyComboBox").IsEnabled);
            Check("existing speech choices retained", Find<ComboBox>("ReadingHighlightModeComboBox").Items.Count == 3 && Find<ComboBox>("SystemVoiceComboBox").Items.Count > 0);
            window.Width = 760; window.Height = 480; await Pause();
            var scroll = Find<ScrollViewer>("DetailScroll");
            var navPosition = nav.PointToScreen(new Point(0, 0));
            scroll.ScrollToBottom(); await Pause();
            Check("right content scrolls independently", scroll.VerticalOffset > 0 && nav.PointToScreen(new Point(0, 0)) == navPosition);
            await Click((FrameworkElement)nav.Items[1]);
            Check("category switch resets content scroll", scroll.VerticalOffset == 0);
            window.Width = 760; window.Height = 480; await Pause();
            Check("minimum size keeps footer reachable", IsOnScreen(ById(window, "SettingsSave"), window));
            await Click((FrameworkElement)nav.Items[2]); scroll.ScrollToBottom(); await Pause();
            Check("minimum size last speech control reachable by scrolling", IsOnScreen(Find<ComboBox>("ReadingHighlightModeComboBox"), window));
            Capture(window, Path.Combine(root, "speech-compact.png"));
            window.Width = 1000; window.Height = 700; await Pause();
            await Click((FrameworkElement)nav.Items[7]); scroll.ScrollToBottom(); await Pause();
            await Click(ById(window, "SettingsLegacyOpen"));
            var legacy = Application.Current.Windows.OfType<LegacySettingsWindow>().Single(); await Pause();
            Check("legacy is separate activating owned window", legacy.Owner == window && legacy.IsActive);
            Check("legacy retains original tabbed view", Descendants(legacy).OfType<TabControl>().Single().Items.Count == 6);
            Check("legacy has no recursive migration button", !Descendants(legacy).Any(e => AutomationProperties.GetAutomationId(e) == "SettingsLegacyOpen"));
            window.Activate(); await Pause(); await Click(ById(window, "SettingsLegacyOpen"));
            Check("reopening reuses one legacy window", Application.Current.Windows.OfType<LegacySettingsWindow>().Count() == 1 && legacy.IsActive);
            var legacyTabs = Descendants(legacy).OfType<TabControl>().Single();
            legacyTabs.SelectedIndex = 3; await Pause();
            await Click((CheckBox)legacy.FindName("ErrorTrackingEnabledCheckBox"));
            await Click(Descendants(legacy).OfType<Button>().Single(b => Equals(b.Content, "Gem")));
            Check("legacy save closes only legacy", !legacy.IsVisible && window.IsVisible);
            window.Activate(); await Pause(); await Click((FrameworkElement)nav.Items[3]);
            await Click(Find<CheckBox>("GlobalSuggestionsCheckBox"));
            Check("primary changes are draft before Save", store.Load().IsGlobalCaptureEnabled);
            await Click(ById(window, "SettingsSave"));
            var saved = new WindowsAppSettingsService().Load();
            Check("both windows' independent changes survive Save", !saved.IsGlobalCaptureEnabled && !saved.IsErrorTrackingEnabled);
            Check("unknown property preserved through UI saves", saved.AdditionalProperties["futureUiOption"].GetString() == "preserve");
            Check("unavailable voice preserved without explicit edit", saved.SystemVoiceIdOverrideByLanguage["da-dk"] == "not-installed-voice-canary");
            Check("runtime global setting applied", !vm.IsGlobalCaptureEnabled);
            window = new SettingsWindow(vm); window.Show(); window.Activate(); await Pause();
            nav = (ListBox)window.FindName("CategoryNavigation");
            await Click((FrameworkElement)nav.Items[3]); await Click((CheckBox)window.FindName("GlobalSuggestionsCheckBox"));
            await Click(ById(window, "SettingsCancel"));
            Check("Cancel discards draft", !store.Load().IsGlobalCaptureEnabled && !vm.IsGlobalCaptureEnabled);
            window = new SettingsWindow(vm); window.Show(); window.Activate(); await Pause();
            Check("new dialog restores saved settings", !((CheckBox)window.FindName("GlobalSuggestionsCheckBox")).IsChecked!.Value);
            var corrupt = "{bad-settings-canary"; File.WriteAllText(store.SettingsPath, corrupt);
            await Click(ById(window, "SettingsSave"));
            Check("failed save keeps window and draft open", window.IsVisible && ((TextBlock)window.FindName("SettingsSaveStatus")).Text.Length > 0);
            Check("failed save leaves source bytes untouched", File.ReadAllText(store.SettingsPath) == corrupt);
            Key(0x1B); await Pause(); Check("Escape dismisses ordinary settings", !window.IsVisible);
            Console.WriteLine($"PASS SETTINGS UI {checks} checks; interactive WPF desktop, ordinary user.");
        }
        finally
        {
            foreach (var item in Application.Current.Windows.Cast<Window>().ToArray()) item.Close();
            if (originalForeground != IntPtr.Zero) SetForegroundWindow(originalForeground);
            SetCursorPos(originalCursor.X, originalCursor.Y);
        }
    }

    private static IEnumerable<FrameworkElement> Descendants(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is FrameworkElement e) yield return e;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
    private static FrameworkElement ById(Window window, string id) => Descendants(window).Single(e => AutomationProperties.GetAutomationId(e) == id);
    private static Button Help(Window window, string id) => (Button)ById(window, "SettingsHelp_" + id);
    private static int OpenHelpCount() => PresentationSource.CurrentSources.Cast<PresentationSource>().Count(s => s.RootVisual is DependencyObject root && Descendants(root).Any(e => AutomationProperties.GetAutomationId(e) == "SettingsHelpContent" && e.IsVisible));
    private static bool HelpOpen() => OpenHelpCount() > 0;
    private static bool IsOnScreen(FrameworkElement element, Window window)
    {
        var rect = new Rect(window.PointToScreen(new Point()), new Size(window.ActualWidth, window.ActualHeight));
        return element.IsVisible && rect.Contains(element.PointToScreen(new Point(element.ActualWidth / 2, element.ActualHeight / 2)));
    }
    private static async Task Click(FrameworkElement element)
    {
        if (!element.IsVisible || !element.IsEnabled) throw new Exception("Mouse target unavailable: " + element.Name);
        var point = element.PointToScreen(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
        SetCursorPos((int)point.X, (int)point.Y); mouse_event(2, 0, 0, 0, UIntPtr.Zero); mouse_event(4, 0, 0, 0, UIntPtr.Zero);
        await Pause();
    }
    private static Task Pause() => Task.Delay(220);
    private static void Capture(Window window, string path)
    {
        var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(window);
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        using var output = File.Create(path); encoder.Save(output);
    }
    private static void Key(byte key) { keybd_event(key, 0, 0, UIntPtr.Zero); keybd_event(key, 0, 2, UIntPtr.Zero); }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X; public int Y; }
}
