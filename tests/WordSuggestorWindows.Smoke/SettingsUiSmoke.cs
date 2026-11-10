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
        var initial = new AppSettingsSnapshot { InterfaceLocalePreference = "da-DK" };
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
            Check("live view has Close without global Save or Cancel", !Descendants(window).OfType<Button>().Any(b => Equals(b.Content, "Gem") || Equals(b.Content, "Annuller")));
            var interfaceLanguage = Find<ComboBox>("InterfaceLanguageComboBox");
            Check("three independent interface choices", interfaceLanguage.Items.Count == 3 && Equals(interfaceLanguage.SelectedValue, "da-DK"));
            await Click(Find<CheckBox>("FollowCaretOptionCheckBox"));
            Check("permission off immediately saves and stops caret requests", !vm.FollowCaretOptionEnabled && vm.IsStaticPlacementMode && !vm.RequestsCaretGeometry && !store.Load().FollowCaretOptionEnabled);
            await Click(Find<CheckBox>("FollowCaretOptionCheckBox"));
            Check("permission on keeps static choice", vm.FollowCaretOptionEnabled && vm.IsStaticPlacementMode && !vm.RequestsCaretGeometry);
            vm.SetSuggestionPlacementMode(SuggestionPlacementMode.FollowCaret);
            Check("permitted explicit follow is persisted", vm.RequestsCaretGeometry && store.Load().SuggestionPlacementMode == "followCaret");
            await Select(interfaceLanguage, 2);
            Check("English interface saved and applied immediately", SettingsText.ResolvedLocale == "en-GB" && store.Load().InterfaceLocalePreference == "en-GB" && window.Title == "WordSuggestor Settings");
            Check("category title and accessible name update without closing", Find<TextBlock>("CategoryTitle").Text == "General" && AutomationProperties.GetName((ListBoxItem)nav.Items[2]) == "Read aloud");
            Check("English help updates with the interface", AutomationProperties.GetName(Help(window, "G01")) == "About Interface language");
            Capture(window, Path.Combine(root, "general-english.png"));
            Check("UI language leaves writing language and voice untouched", vm.SelectedLanguageOption.LanguageCode == "da-DK" && store.Load().SelectedLanguageCode == "da-DK" && store.Load().SystemVoiceIdOverrideByLanguage["da-dk"] == "not-installed-voice-canary");
            Check("Danish Core suggestions still work with English interface", (await provider.SuggestAsync("skri", default)).Any(s => s.Term == "skrive"));
            var writingLanguage = Find<ComboBox>("LanguageComboBox");
            writingLanguage.IsDropDownOpen = true; await Pause();
            Check("unavailable language packs cannot be selected", vm.LanguageOptions.Select((l, i) => (l, i)).All(p => ((ComboBoxItem)writingLanguage.ItemContainerGenerator.ContainerFromIndex(p.i)).IsEnabled == p.l.IsPackAvailable));
            writingLanguage.Focus(); Key(0x1B); await Pause();
            Check("Escape closes the native dropdown before the settings window", !writingLanguage.IsDropDownOpen && window.IsVisible);
            await Select(interfaceLanguage, 1);
            Check("Danish interface restored without changing category or writing language", Find<TextBlock>("CategoryTitle").Text == "Generelt" && nav.SelectedIndex == 1 && vm.SelectedLanguageOption.LanguageCode == "da-DK");
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
            Check("minimum size keeps footer reachable", IsOnScreen(ById(window, "SettingsClose"), window));
            await Click((FrameworkElement)nav.Items[2]); scroll.ScrollToBottom(); await Pause();
            Check("minimum size last speech control reachable by scrolling", IsOnScreen(Find<ComboBox>("ReadingHighlightModeComboBox"), window));
            Capture(window, Path.Combine(root, "speech-compact.png"));
            window.Width = 1000; window.Height = 700; await Pause();
            await Click((FrameworkElement)nav.Items[7]); scroll.ScrollToBottom(); await Pause();
            await Click(ById(window, "SettingsLegacyOpen"));
            var legacy = Application.Current.Windows.OfType<LegacySettingsWindow>().Single(); await Pause();
            ArrangeConcurrentWindows(window, legacy); await Pause();
            Check("legacy is separate activating owned window", legacy.Owner == window && legacy.IsActive);
            Check("legacy retains original tabbed view", Descendants(legacy).OfType<TabControl>().Single().Items.Count == 6);
            Check("legacy has no recursive migration button", !Descendants(legacy).Any(e => AutomationProperties.GetAutomationId(e) == "SettingsLegacyOpen"));
            window.Activate(); await Pause(); await Click(ById(window, "SettingsLegacyOpen"));
            Check("reopening reuses one legacy window", Application.Current.Windows.OfType<LegacySettingsWindow>().Count() == 1 && legacy.IsActive);
            var legacyTabs = Descendants(legacy).OfType<TabControl>().Single();
            legacyTabs.SelectedIndex = 3; await Pause();
            await Click((CheckBox)legacy.FindName("ErrorTrackingEnabledCheckBox"));
            Check("legacy keeps explicit draft until Save", store.Load().IsErrorTrackingEnabled);
            window.Activate(); await Pause(); await Click((FrameworkElement)nav.Items[3]);
            await Click(Find<CheckBox>("GlobalSuggestionsCheckBox"));
            Check("primary preference applied before closing", !store.Load().IsGlobalCaptureEnabled && !vm.IsGlobalCaptureEnabled && window.IsVisible);
            legacy.Activate(); await Pause();
            await Click(Descendants(legacy).OfType<Button>().Single(b => Equals(b.Content, "Gem")));
            Check("legacy save closes only legacy", !legacy.IsVisible && window.IsVisible);
            var saved = new WindowsAppSettingsService().Load();
            Check("legacy Save preserves newer primary change", !saved.IsGlobalCaptureEnabled && !saved.IsErrorTrackingEnabled);
            Check("legacy Save refreshes primary effective state", !Find<CheckBox>("ErrorTrackingEnabledCheckBox").IsChecked!.Value);
            Check("unknown property preserved through UI saves", saved.AdditionalProperties["futureUiOption"].GetString() == "preserve");
            Check("unavailable voice preserved without explicit edit", saved.SystemVoiceIdOverrideByLanguage["da-dk"] == "not-installed-voice-canary");
            Check("runtime global setting applied", !vm.IsGlobalCaptureEnabled);
            window.Activate(); await Pause(); await Click(ById(window, "SettingsClose"));
            window = new SettingsWindow(vm); window.Show(); window.Activate(); await Pause();
            nav = (ListBox)window.FindName("CategoryNavigation");
            await Click((FrameworkElement)nav.Items[3]); await Click((CheckBox)window.FindName("GlobalSuggestionsCheckBox"));
            Key(0x1B); await Pause();
            Check("Close and Escape retain live preferences", store.Load().IsGlobalCaptureEnabled && vm.IsGlobalCaptureEnabled);
            window = new SettingsWindow(vm); window.Show(); window.Activate(); await Pause();
            Check("new dialog restores live-saved settings", ((CheckBox)window.FindName("GlobalSuggestionsCheckBox")).IsChecked!.Value);
            nav = (ListBox)window.FindName("CategoryNavigation");
            await Click((FrameworkElement)nav.Items[7]); Find<ScrollViewer>("DetailScroll").ScrollToBottom(); await Pause();
            await Click(ById(window, "SettingsLegacyOpen")); await Pause();
            legacy = Application.Current.Windows.OfType<LegacySettingsWindow>().Single();
            ArrangeConcurrentWindows(window, legacy); await Pause();
            ((ComboBox)legacy.FindName("ReadingHighlightModeComboBox")).BringIntoView(); await Pause();
            await Select((ComboBox)legacy.FindName("ReadingHighlightModeComboBox"), 2);
            window.Activate(); await Pause(); await Click((FrameworkElement)nav.Items[2]); Find<ScrollViewer>("DetailScroll").ScrollToBottom(); await Pause();
            await Select(Find<ComboBox>("ReadingHighlightModeComboBox"), 0);
            legacy.Activate(); await Pause(); await Click(Descendants(legacy).OfType<Button>().Single(b => Equals(b.Content, "Gem")));
            Check("conflicting legacy Save keeps draft and reports failure", legacy.IsVisible && ((TextBlock)legacy.FindName("SettingsSaveStatus")).Text.Contains("andet vindue"));
            Check("conflicting legacy draft does not overwrite live value", store.Load().ReadingHighlightMode == "none");
            await Click(Descendants(legacy).OfType<Button>().Single(b => Equals(b.Content, "Annuller")));
            Check("legacy Cancel discards only its draft", !legacy.IsVisible && window.IsVisible && store.Load().ReadingHighlightMode == "none");
            window.Activate(); await Pause(); await Click((FrameworkElement)nav.Items[6]);
            var beforeFailure = File.ReadAllText(store.SettingsPath);
            using (var locked = new FileStream(store.SettingsPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await Click(Find<CheckBox>("ErrorTrackingEnabledCheckBox"));
                Check("I/O failure reverts live control without closing", window.IsVisible && Find<CheckBox>("ErrorTrackingEnabledCheckBox").IsChecked == false && Find<TextBlock>("SettingsSaveStatus").Text.Contains("ikke gemmes"));
                Check("failed atomic replace preserves stored bytes", File.ReadAllText(store.SettingsPath) == beforeFailure);
            }
            var corrupt = "{bad-settings-canary"; File.WriteAllText(store.SettingsPath, corrupt);
            await Click(Find<CheckBox>("ErrorTrackingEnabledCheckBox"));
            Check("corrupt-file save rejected and effective control restored", window.IsVisible && Find<CheckBox>("ErrorTrackingEnabledCheckBox").IsChecked == false && Find<TextBlock>("SettingsSaveStatus").Text.Length > 0);
            Check("failed save leaves source bytes untouched", File.ReadAllText(store.SettingsPath) == corrupt);
            Key(0x1B); await Pause(); Check("Escape dismisses ordinary settings", !window.IsVisible);
            window = new SettingsWindow(vm); window.Show(); window.Activate(); await Pause();
            Check("reopen after file failure shows last effective state and warning", ((CheckBox)window.FindName("GlobalSuggestionsCheckBox")).IsChecked == vm.IsGlobalCaptureEnabled &&
                ((TextBlock)window.FindName("SettingsSaveStatus")).Text == SettingsText.Get("UnavailableData"));
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
        var owner = Window.GetWindow(element);
        if (owner is not null && GetAncestor(WindowFromPoint(new NativePoint { X = (int)point.X, Y = (int)point.Y }), 2) != new System.Windows.Interop.WindowInteropHelper(owner).Handle)
            throw new Exception("Mouse target is covered by another window: " + element.Name);
        SetCursorPos((int)point.X, (int)point.Y); mouse_event(2, 0, 0, 0, UIntPtr.Zero); mouse_event(4, 0, 0, 0, UIntPtr.Zero);
        await Pause();
    }
    private static Task Pause() => Task.Delay(220);
    private static void ArrangeConcurrentWindows(Window primary, Window legacy)
    {
        // Owned windows remain above their owner. Put these synthetic windows
        // side-by-side before testing actual mouse input into both of them.
        primary.Width = 760; primary.Left = SystemParameters.WorkArea.Left + 8; primary.Top = SystemParameters.WorkArea.Top + 8;
        legacy.Width = 680; legacy.Left = primary.Left + primary.ActualWidth + 8; legacy.Top = primary.Top;
    }
    private static async Task Select(ComboBox combo, int index)
    {
        combo.BringIntoView(); combo.Focus(); await Pause();
        Key(0x24); await Pause(); // Home then native arrows, including live change events
        for (var i = 0; i < index; i++) { Key(0x28); await Pause(); }
    }
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
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(NativePoint point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X; public int Y; }
}
