using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using WordSuggestorWindows.App;
using WordSuggestorWindows.App.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

internal static class SuggestionSettingsSmoke
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static int _count;
    private static void Check(string title, bool passed) { if (!passed) throw new Exception(title); _count++; Console.WriteLine("PASS SUGGESTION SETTINGS " + title); }
    public static async Task Run(string repo, bool interactive = true)
    {
        var root = Path.Combine(repo, ".artifacts", "tests", "suggestions-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", root);
        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/WordSuggestorWindows.App;component/SharedStyles.xaml", UriKind.Relative) });
        var path = Path.Combine(root, "settings.json"); var store = new WindowsAppSettingsService(path);
        const string old = "{\"schemaVersion\":3,\"readingSpeedDelta\":0.15,\"storeSentenceExamples\":false,\"learningConsent\":false}";
        File.WriteAllText(path, old); var bytes = File.ReadAllBytes(path); var migrated = store.LoadResult();
        Check("schema 3 migration does not write", migrated.State == SettingsLoadState.Migrated && bytes.SequenceEqual(File.ReadAllBytes(path)));
        var settings = migrated.Settings; settings.Suggestions = settings.Suggestions with { Spacing = "never", SpeakOnHover = false, IdleFadeSeconds = 31 }; store.Save(settings);
        Check("schema 3 byte-exact backup and no repeated speed migration", File.ReadAllBytes(path + ".v3.bak").SequenceEqual(bytes) && Math.Abs(store.Load().ReadingSpeedDelta - .15) < 1e-9);
        Check("false privacy choices survive", !store.Load().StoreSentenceExamples && !store.Load().AdditionalProperties["learningConsent"].GetBoolean());
        var restart = new WindowsAppSettingsService(path).Load();
        Check("suggestion preferences persist on restart", restart.Suggestions.Spacing == "never" && !restart.Suggestions.SpeakOnHover && restart.Suggestions.IdleFadeSeconds == 31);
        var other = restart.Clone(); other.Suggestions = other.Suggestions with { Density = "wordOnly" };
        var draft = restart.Clone(); draft.Suggestions = draft.Suggestions with { EditorWrap = false };
        var merged = SettingsPatch.Merge(restart, draft, other);
        Check("concurrent fields merge inside suggestion preferences", merged.Suggestions.Density == "wordOnly" && !merged.Suggestions.EditorWrap);
        foreach (var bad in new[] { restart.Suggestions with { Spacing = "bad" }, restart.Suggestions with { PrimaryShortcut = "ctrl" }, restart.Suggestions with { Responsiveness = 5 }, restart.Suggestions with { IdleFadeSeconds = -1 }, restart.Suggestions with { PrimaryDomains = null! } })
        {
            var rejected = false; try { var invalid = restart.Clone(); invalid.Suggestions = bad; SettingsSchema.Upgrade(invalid); } catch (InvalidOperationException) { rejected = true; }
            Check("invalid preferences fail closed", rejected);
        }
        var receipt = new SmartCommitReceipt("hej ", 4, 1000);
        Check("smart receipt accepts immediate exact input", receipt.MatchInput("hej ,", 5, 1100) == ",");
        Check("smart receipt rejects target text/caret/TTL mismatch", receipt.MatchInput("nej ,", 5, 1100) is null && receipt.MatchInput("hej ,", 4, 1100) is null && receipt.MatchInput("hej ,", 5, 2501) is null);
        Check("exact Core insertion keeps prefix/suffix and caret", EditorCommit.Replace("😀 skri,", 7, 7, "skrive", true) == ("😀 skrive,", 9));
        foreach (var (density, height) in new[] { ("wordOnly", 34d), ("wordAndOrigin", 40d), ("fullDetails", 48d) })
        {
            var entry = new SuggestionOverlayEntry(0, "Ctrl+1", new("test", 1, "prefix", "word", "noun", "sb"), false) { Density = density };
            Check("fixed row density " + density, entry.RowHeight == height && entry.HasMetadataSummary == (density == "fullDetails") && (entry.KindInlineSummary.Length > 0) == (density != "wordOnly"));
        }
        for (var profile = 0; profile < 5; profile++)
        {
            var slow = new TimedProvider();
            var profileStore = new WindowsAppSettingsService(Path.Combine(root, "profile" + profile + ".json"));
            profileStore.Save(new() { Suggestions = new() { Responsiveness = profile } });
            using var timed = new MainWindowViewModel(slow, new WindowsErrorInsightsStore(), profileStore);
            timed.SetLocalEditorActive(true); var watch = Stopwatch.StartNew();
            for (var i = 1; i <= 12; i++) { timed.CaretIndex = i; timed.EditorText = new string('s', i); await Task.Delay(10); }
            var lastInput = watch.ElapsedMilliseconds;
            await Until(() => timed.Suggestions.Count > 0);
            var policy = new SuggestionSchedulingPolicy(); var delays = Enumerable.Range(0, 12).Select(i => policy.NextDelay(profile, i * 10)).ToArray();
            Check("profile scheduling " + profile, delays[0] == new[] { 20, 35, 55, 85, 110 }[profile] && delays[^1] == new[] { 140, 200, 260, 380, 500 }[profile] && slow.MaxActive == 1 && slow.Completed == 1);
            Console.WriteLine($"MEASURE SUGGESTION profile={profile} inputs=12 cadence_ms=10 requests={slow.Requests} max_active={slow.MaxActive} latest_result_after_last_input_ms={watch.ElapsedMilliseconds-lastInput}");
        }
        var provider = new WordSuggestorCoreCliSuggestionProvider(localDataRoot: root);
        using var vm = new MainWindowViewModel(provider, new WindowsErrorInsightsStore(), store);
        var caps = await provider.GetCapabilitiesAsync();
        Check("real Core commit plans available without invented pack data", caps.CommitPlans && !caps.ContextReranking && !caps.WordFamilies && !caps.LivePeriodAssist);
        var scope = new CoreRequestContext(Guid.NewGuid(), 1);
        Check("adapter smart punctuation and excluded hyphen", (await provider.GetSmartFollowupAsync(",", scope)).Value.Replacement == "," && (await provider.GetSmartFollowupAsync("-", scope)).Value.Replacement is null);
        vm.CanCommitLocally = () => true; vm.SetLocalEditorActive(true);
        foreach (var mode in new[] { "never", "always", "smart" })
        {
            settings = store.Load(); settings.Suggestions = settings.Suggestions with { Spacing = mode }; store.Save(settings);
            vm.CaretIndex = 4; vm.EditorText = "skri, suffix"; vm.UpdateEditorSelection(4, 4);
            await Until(() => vm.Suggestions.Count > 0 && !vm.IsBusy);
            var item = vm.VisibleSuggestions[0].Suggestion;
            var expected = mode == "always" ? item.Term + " , suffix" : item.Term + ", suffix";
            Check("real Core plan reaches verified local commit " + mode, vm.AcceptSuggestion(item) && vm.EditorText == expected && vm.CaretIndex == item.CommitPlan!.Text.Length);
        }
        vm.CaretIndex = 4; vm.EditorText = "skri"; vm.UpdateEditorSelection(4, 4); await Until(() => vm.Suggestions.Count > 0 && !vm.IsBusy);
        var stale = vm.Suggestions[0]; vm.EditorText = "skri changed";
        Check("changed document rejects retained candidate", !vm.AcceptSuggestion(stale));
        vm.SetLocalEditorActive(false); vm.EditorText = ""; vm.CaretIndex = 0;
        settings = store.Load(); settings.Suggestions = settings.Suggestions with { Spacing = "never" }; store.Save(settings);
        if (!interactive) { Console.WriteLine($"PASS SUGGESTION STATE {_count} checks; desktop integration not run."); return; }
        SettingsWindow? window = null; MainWindow? main = null; IntPtr notepad = IntPtr.Zero;
        var foreground = GetForegroundWindow(); GetCursorPos(out var cursor);
        try
        {
            window = new SettingsWindow(vm); window.Show(); window.Activate();
            ((ListBox)window.FindName("CategoryNavigation")).SelectedIndex = 3; await Task.Delay(500);
            var rows = Descendants(window).OfType<SettingsRow>().Where(r => r.ControlId.StartsWith('F')).ToArray();
            Check("all twenty suggestion controls present", rows.Length == 20 && rows.Select(r => r.ControlId).Distinct().Count() == 20);
            Check("all controls have meaningful screen-reader labels", rows.All(r => !string.IsNullOrWhiteSpace(r.Header?.ToString())));
            Check("missing data gated without erasing intent", !((CheckBox)window.FindName("ContextBased")).IsEnabled && ((CheckBox)window.FindName("ContextBased")).IsChecked == true && !((CheckBox)window.FindName("LivePeriodAssist")).IsEnabled);
            ((ComboBox)window.FindName("SuggestionDensity")).SelectedValue = "wordOnly";
            ((ComboBox)window.FindName("OrdinaryShortcut")).SelectedValue = "ctrlShift";
            Check("shortcut conflict swaps atomically", store.Load().Suggestions.OrdinaryShortcut == "ctrlShift" && store.Load().Suggestions.PrimaryShortcut == "ctrl");
            ((ComboBox)window.FindName("OrdinaryShortcut")).SelectedValue = "ctrl";
            Check("live density saved", store.Load().Suggestions.Density == "wordOnly");
            window.Close(); window = null;
            main = new MainWindow(vm); main.Show(); main.OpenEditor(); await Task.Delay(300);
            var editorWindow = Field<InternalEditorWindow>(main, "_editorWindow");
            RichTextBox Editor() => (RichTextBox)typeof(MainWindow).GetProperty("EditorTextBox", Private)!.GetValue(main)!;
            var handle = new WindowInteropHelper(editorWindow).Handle;
            async Task Seed(string text)
            {
                vm.CaretIndex = text.Length; vm.EditorText = text;
                editorWindow.Activate(); Editor().Focus(); Editor().CaretPosition = Editor().Document.ContentEnd;
                vm.UpdateEditorSelection(text.Length, text.Length); await Until(() => vm.Suggestions.Count > 0 && !vm.IsBusy);
                SetForegroundWindow(handle); editorWindow.Activate(); Editor().Focus();
                Console.WriteLine($"DIAGNOSTIC suggestion editor handle={handle} foreground={GetForegroundWindow()} active={editorWindow.IsActive} focus={Editor().IsKeyboardFocusWithin} visible={editorWindow.IsVisible}");
                await Until(() => GetForegroundWindow() == handle && Editor().IsKeyboardFocusWithin);
            }
            await Seed("skri");
            var before = vm.EditorText; var caretBefore = vm.CaretIndex;
            settings = store.Load(); settings.Suggestions = settings.Suggestions with { EditorWrap = false }; store.Save(settings);
            Check("unwrapped editor preserves text/caret", Editor().Document.PageWidth == 100000 && vm.EditorText == before && vm.CaretIndex == caretBefore);
            settings = store.Load(); settings.Suggestions = settings.Suggestions with { EditorWrap = true }; store.Save(settings);
            Check("wrapped editor restored without replacing document", double.IsNaN(Editor().Document.PageWidth) && vm.EditorText == before && vm.CaretIndex == caretBefore);
            foreach (var mode in new[] { "never", "always", "smart" })
            {
                settings = store.Load(); settings.Suggestions = settings.Suggestions with { Spacing = mode }; store.Save(settings);
                await Seed("skri"); var item = vm.VisibleSuggestions[0].Suggestion;
                Chord(handle, 0x11, 0x31); await Task.Delay(100);
                Check("physical keyboard Core insertion " + mode, item.CommitPlan is not null && vm.EditorText == item.CommitPlan.Text && vm.CaretIndex == vm.EditorText.Length);
            }
            await Seed("skri");
            var overlay = Field<SuggestionOverlayWindow>(main, "_overlayWindow");
            var candidate = vm.VisibleSuggestions[0].Suggestion;
            await Click(Descendants(overlay).OfType<Button>().First(b => System.Windows.Automation.AutomationProperties.GetAutomationId(b) == "AcceptCandidate"));
            Check("physical mouse uses identical Core commit", vm.EditorText == candidate.CommitPlan!.Text && vm.CaretIndex == vm.EditorText.Length && GetForegroundWindow() == handle);
            // Safe receipt correction is tested through the same WPF edit event as typing.
            Chord(handle, 0xBC);
            await Task.Delay(400);
            Check("smart followup removes only verified auto-space", vm.EditorText == candidate.Term + ",");
            settings = store.Load(); settings.Suggestions = settings.Suggestions with { IdleFadeSeconds = 0 }; store.Save(settings);
            await Seed("skri"); await Task.Delay(150);
            Check("fading overlay rejects hidden keyboard commits", !vm.IsSuggestionInteractionEnabled && !vm.AcceptSuggestionAtIndex(0));
            await Task.Delay(3200);
            Check("idle panel actually hides", !overlay.IsVisible);
            settings = store.Load(); settings.Suggestions = settings.Suggestions with { IdleFadeSeconds = 31 }; store.Save(settings);
            await Seed("skri");
            Check("fresh input restores panel", overlay.IsVisible && vm.IsSuggestionInteractionEnabled);
            // No seed catalog is installed implicitly by the application.
            Check("no production domain catalog is auto-promoted", vm.ResolveDomainCatalog() is null);
            var catalogRoot = Path.Combine(root, "Packs", "da-DK", "specialized"); Directory.CreateDirectory(catalogRoot);
            var seedRoot = Path.Combine(Directory.GetParent(repo)!.FullName, "WordSuggestorCore", "LanguagePacks", "da_DK", "data", "raw", "specialized", "ai_ml_llm_v1");
            foreach (var id in new[] { "test_primary", "test_secondary" })
            {
                var destination = Path.Combine(catalogRoot, id); Directory.CreateDirectory(destination);
                foreach (var file in Directory.EnumerateFiles(seedRoot)) File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
                var manifest = Path.Combine(destination, "manifest.json"); var json = JsonNode.Parse(File.ReadAllText(manifest))!;
                json["domainID"] = id; json["title"] = "TEST ONLY " + id; File.WriteAllText(manifest, json.ToJsonString());
            }
            File.WriteAllText(Path.Combine(catalogRoot, "catalog.json"), "{\"language\":\"da_DK\",\"entries\":[\"test_primary/manifest.json\",\"test_secondary/manifest.json\"]}");
            settings = store.Load(); settings.IsDomainListsEnabled = true; settings.Suggestions = settings.Suggestions with { PrimaryDomains = new() { ["da-DK"] = "test_primary" }, SecondaryDomains = new() { ["da-DK"] = "test_secondary" } }; store.Save(settings);
            await Seed("trans");
            Check("explicit test catalog populates all three bounded lanes", vm.VisiblePrimarySuggestions.Count is > 0 and <= 10 && vm.VisibleSecondarySuggestions.Count is > 0 and <= 10 && vm.SuggestionOverlayWidth == 876);
            var primary = vm.VisiblePrimarySuggestions[0].Suggestion;
            Chord(handle, 0x11, 0x10, 0x31); await Task.Delay(150);
            Check("physical Ctrl Shift digit chooses primary lane", vm.EditorText == primary.CommitPlan!.Text);
            await Seed("trans"); var secondary = vm.VisibleSecondarySuggestions[0].Suggestion;
            Chord(handle, 0x12, 0x31); await Task.Delay(150);
            Check("physical Alt digit chooses secondary lane", vm.EditorText == secondary.CommitPlan!.Text);
            // Real external host. Never modify a pre-existing Notepad document.
            var filePath = Path.Combine(root, "notepad-suggestions-" + Guid.NewGuid().ToString("N") + ".txt"); File.WriteAllText(filePath, "skri");
            Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = false, ArgumentList = { filePath } });
            await Until(() => (notepad = FindWindow(Path.GetFileNameWithoutExtension(filePath))) != IntPtr.Zero);
            settings = store.Load(); settings.IsGlobalCaptureEnabled = true; store.Save(settings);
            SetForegroundWindow(notepad); await Task.Delay(200); Chord(notepad, 0x11, 0x23);
            await Until(() => vm.IsExternalSuggestionSessionActive && vm.Suggestions.Count > 0 && !vm.IsBusy);
            var externalCandidate = vm.VisibleSuggestions[0].Suggestion;
            Chord(notepad, 0x11, 0x31);
            var adapter = new WindowsNativeTextAdapter();
            await Until(() => adapter.ReadFocused()?.Text == externalCandidate.CommitPlan?.Text);
            Check("Notepad exact Core insertion and caret", adapter.ReadFocused() is { } target && target.Start == target.Text.Length && target.End == target.Start);
            Check("external mouse and keyboard preserve foreground", GetForegroundWindow() == notepad);
        }
        finally
        {
            window?.Close(); if (notepad != IntPtr.Zero) PostMessage(notepad, 0x10, IntPtr.Zero, IntPtr.Zero);
            if (main is not null)
            {
                vm.EditorText = "";
                if (typeof(MainWindow).GetField("_workspace", Private)?.GetValue(main) is { } workspace)
                    foreach (var tab in (IEnumerable)workspace.GetType().GetProperty("Tabs")!.GetValue(workspace)!) tab.GetType().GetMethod("UpdateText")!.Invoke(tab, [""]);
                main.Close();
            }
            if (foreground != IntPtr.Zero) SetForegroundWindow(foreground); SetCursorPos(cursor.X, cursor.Y);
        }
        Console.WriteLine($"PASS SUGGESTION SETTINGS {_count} checks.");
    }
    private sealed class TimedProvider : ISuggestionProvider
    {
        private int _active; public int Requests, Completed, MaxActive;
        public string ProviderDescription => "timed scheduling fixture"; public string? HealthWarning => null;
        public IReadOnlyList<LanguageOption> LanguageOptions { get; } = [new("da-DK", "DA", "Dansk", "da_DK", "da_lexicon.sqlite") { PackPath = "fixture" }];
        public LanguageOption SelectedLanguage => LanguageOptions[0]; public void SetLanguage(LanguageOption language) { }
        public Task AddUserWordAsync(string word, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public async Task<IReadOnlyList<SuggestionItem>> SuggestAsync(string text, CancellationToken token)
        {
            Requests++; MaxActive = Math.Max(MaxActive, Interlocked.Increment(ref _active));
            try { await Task.Delay(80, token); Completed++; return [new(text + "x", 1, "prefix", "word", null, null)]; }
            finally { Interlocked.Decrement(ref _active); }
        }
    }
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private)!.GetValue(target)!;
    private static IEnumerable<FrameworkElement> Descendants(DependencyObject target)
    { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(target); i++) { var child = VisualTreeHelper.GetChild(target, i); if (child is FrameworkElement element) yield return element; foreach (var nested in Descendants(child)) yield return nested; } }
    private static async Task Until(Func<bool> condition, int timeout = 8000)
    { var clock = Stopwatch.StartNew(); while (!condition()) { if (clock.ElapsedMilliseconds > timeout) throw new Exception("Timed out waiting for suggestion condition."); await Task.Delay(20); } }
    private static async Task Click(FrameworkElement element)
    {
        var point = element.PointToScreen(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
        SetCursorPos((int)point.X, (int)point.Y); mouse_event(2, 0, 0, 0, UIntPtr.Zero); mouse_event(4, 0, 0, 0, UIntPtr.Zero); await Task.Delay(100);
    }
    private static void Chord(IntPtr target, params byte[] keys)
    {
        if (GetForegroundWindow() != target) throw new Exception("Synthetic input target changed.");
        try { foreach (var key in keys) keybd_event(key, 0, 0, UIntPtr.Zero); }
        finally { foreach (var key in keys.Reverse()) keybd_event(key, 0, 2, UIntPtr.Zero); }
    }
    private static IntPtr FindWindow(string title)
    { var result = IntPtr.Zero; EnumWindows((handle, _) => { var text = new StringBuilder(512); GetWindowText(handle, text, 512); if (text.ToString().Contains(title)) result = handle; return true; }, IntPtr.Zero); return result; }
    private delegate bool Callback(IntPtr handle, IntPtr argument);
    [StructLayout(LayoutKind.Sequential)] private struct Cursor { public int X, Y; }
    [DllImport("user32.dll")] private static extern bool EnumWindows(Callback callback, IntPtr argument);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr handle, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Cursor point);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
