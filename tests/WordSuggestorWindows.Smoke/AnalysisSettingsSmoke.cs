using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using WordSuggestorWindows.App;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

internal static class AnalysisSettingsSmoke
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static async Task Run(string repo, bool interactive)
    {
        var root = Path.Combine(repo, ".artifacts", "tests", "analysis-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", root);
        var count = 0;
        void Check(string title, bool ok) { if (!ok) throw new Exception(title); count++; Console.WriteLine("PASS ANALYSIS SETTINGS " + title); }
        void Reject(string title, Action action) { var failed = false; try { action(); } catch (InvalidOperationException) { failed = true; } Check(title, failed); }
        var settingsPath = Path.Combine(root, "settings.json"); const string old = "{\"schemaVersion\":4,\"punctuationCommaStyle\":\"Standard\",\"readingSpeedDelta\":0.15,\"storeSentenceExamples\":false,\"unknownConsent\":false}";
        File.WriteAllText(settingsPath, old); var originalBytes = File.ReadAllBytes(settingsPath);
        var store = new WindowsAppSettingsService(settingsPath); var snapshot = store.Load();
        Check("old comma style migrates without granting storage consent", snapshot.SchemaVersion == SettingsSchema.CurrentVersion && snapshot.PunctuationCommaStyle == "grammatical_without_start_comma" && !snapshot.SaveWordChoices && !snapshot.ShareWordChoices && !snapshot.StoreSentenceExamples);
        snapshot.IsPunctuationDiagnosticsEnabled = true; store.Save(snapshot);
        Check("v4 exact backup and unknown false values preserved", File.ReadAllBytes(settingsPath + ".v4.bak").SequenceEqual(originalBytes) && !store.Load().AdditionalProperties["unknownConsent"].GetBoolean());
        Check("invalid contextual mode rolls back to graph-only", SettingsSchema.Upgrade(new() { ContextualCommaMode = "unknown" }).ContextualCommaMode == "legacy_graph_only");
        Check("sharing requires explicit local storage", !SettingsSchema.Upgrade(new() { SaveWordChoices = false, ShareWordChoices = true }).ShareWordChoices);
        Reject("invalid comma style is not silently changed", () => SettingsSchema.Upgrade(new() { PunctuationCommaStyle = "unknown" }));
        var packRoot = Path.Combine(root, "Packs"); Directory.CreateDirectory(packRoot);
        var pack = Path.Combine(packRoot, "da_DK_pack_v1.0.0001.sqlite"); PackFixture.Create(pack, "da_DK");
        PackFixture.Execute(pack, "INSERT INTO term_variants(term_norm,display,gram_tag,pos,freq,lemma_norm,lemma_source) VALUES('test','test','sb','noun',50,'test','fixture'),('test','test','vb.inf','verb',30,'teste','fixture');");
        var provider = new WordSuggestorCoreCliSuggestionProvider(localDataRoot: root);
        Check("real validated synthetic pack selected", provider.SelectedLanguage.PackPath == pack);
        var context = new CoreRequestContext(Guid.NewGuid(), 1);
        var document = await provider.AnalyzeDocumentAsync("😀 a\u030a test,verden", context, new(Punctuation: true));
        Check("document ranges retain original decomposed UTF16", document.Value.Readings.Words.Any(w => w.Start == 3 && w.Length == 2));
        Check("source-backed ambiguity is exposed", document.Value.Readings.Words.Any(w => w.Ambiguous && w.Readings.Select(r => r.WordClass).Distinct().Count() > 1));
        Check("Core supplies bounded sentence example range", document.Value.Diagnostics.Any(d => d.ExampleStart == 0 && d.ExampleLength == "😀 a\u030a test,verden".Length));
        using var vm = new MainWindowViewModel(provider, new WindowsErrorInsightsStore(), store);
        vm.CanCommitLocally = () => true;
        const string text = "PRIVATE_CONTEXT_SENTINEL test,verden";
        vm.CaretIndex = text.Length; vm.EditorText = text;
        await Until(() => vm.AnalysisDocument is not null);
        var word = vm.AnalysisDocument!.Result.Readings.Words.Single(w => w.Ambiguous);
        var reading = word.Readings.Last();
        Check("choice can affect current display without persistence", vm.TryChooseReading(word, reading) && vm.DisplayWordClass(word) == reading.WordClass && vm.LoadSavedChoices().Count == 0);
        snapshot = store.Load(); snapshot.SaveWordChoices = snapshot.ShareWordChoices = true; store.Save(snapshot);
        Check("explicit opt-in records current Core reading", vm.TryChooseReading(word, reading) && vm.LoadSavedChoices().Count == 1);
        var choices = new WindowsWordChoiceStore(); var saved = File.ReadAllText(choices.StorePath);
        Check("choices have no surrounding document or upload eligibility", !saved.Contains("PRIVATE_CONTEXT_SENTINEL") && !vm.LoadSavedChoices()[0].BackendUploadEligible && !vm.LoadSavedChoices()[0].AdaptiveRankingEligible);
        Check("choices survive service restart", new WindowsWordChoiceStore().Load().Single().PackSha256.Length == 64);
        var diagnostic = vm.AnalysisDocument!.Result.Diagnostics.First(d => d.RuleId == "ws_punctuation_space_after_mark");
        Check("verified diagnostic correction applies atomically", vm.TryApplyDiagnostic(diagnostic) && vm.EditorText.Contains(", ") && !vm.TryApplyDiagnostic(diagnostic));
        var insights = new WindowsErrorInsightsStore();
        Check("diagnostic tracking excludes text by default", !File.ReadAllText(insights.StorePath).Contains("PRIVATE_CONTEXT_SENTINEL"));
        snapshot = store.Load(); snapshot.StoreSentenceExamples = true; store.Save(snapshot);
        vm.EditorText = text; await Until(() => vm.AnalysisDocument is not null);
        Check("explicit example opt-in uses exact Core sentence range", vm.TryApplyDiagnostic(vm.AnalysisDocument!.Result.Diagnostics.First(d => d.RuleId == "ws_punctuation_space_after_mark")) && File.ReadAllText(insights.StorePath).Contains("PRIVATE_CONTEXT_SENTINEL"));
        var length = new FileInfo(insights.StorePath).Length;
        snapshot = store.Load(); snapshot.IsErrorTrackingEnabled = false; store.Save(snapshot);
        vm.EditorText = text; await Until(() => vm.AnalysisDocument is not null);
        Check("tracking off prevents new content and metadata", vm.TryApplyDiagnostic(vm.AnalysisDocument!.Result.Diagnostics.First(d => d.RuleId == "ws_punctuation_space_after_mark")) && new FileInfo(insights.StorePath).Length == length);
        var oldAnalysis = vm.AnalysisDocument; vm.EditorText = text + "x";
        Check("text changes invalidate analysis immediately", vm.AnalysisDocument is null && !vm.TryChooseReading(word, reading));
        snapshot = store.Load(); snapshot.IsTextAnalyzerEnabled = false; store.Save(snapshot); await Task.Delay(500);
        Check("master off cancels stale analysis", vm.AnalysisDocument is null);
        var group = new CoreDiagnostic(2, 1, "punctuation", "warning", null, "fixture", 1, null, "paired", [new(2, 0, "X"), new(4, 1, "c")], []);
        var plan = DiagnosticEditPlanner.Create("😀a b", 5, group);
        Check("paired UTF16 edit transaction preserves caret", plan.Result == "😀Xa c" && plan.Caret == 6);
        Reject("overlapping corrections rejected", () => DiagnosticEditPlanner.Create("abc", 3, group with { Edits = [new(0, 2, "x"), new(1, 1, "y")] }));
        Reject("duplicate zero-width corrections rejected", () => DiagnosticEditPlanner.Create("abc", 3, group with { Edits = [new(0, 0, "x"), new(0, 0, "y")] }));
        Reject("surrogate splitting correction rejected", () => DiagnosticEditPlanner.Create("😀a", 3, group with { Edits = [new(1, 1, "x")] }));
        var id = vm.LoadSavedChoices().Single().Id;
        Check("delete one persists without stale backups", vm.DeleteSavedChoice(id) && choices.Load().Count == 0 && !File.Exists(choices.StorePath + ".bak"));
        var userFile = Path.Combine(root, "user-document.txt"); File.WriteAllText(userFile, "KEEP_DOCUMENT");
        await provider.AddUserWordAsync("learningcanary");
        var userWordsPath = Path.Combine(root, "UserWords", "local_lexicon_overlay_v2.json"); var userWordsBefore = File.ReadAllBytes(userWordsPath);
        Check("reset clears only learning-owned scope", vm.ResetLocalLearning() && !store.Load().SaveWordChoices && !store.Load().ShareWordChoices && File.Exists(userFile) && File.Exists(pack) && File.Exists(insights.StorePath) && userWordsBefore.SequenceEqual(File.ReadAllBytes(userWordsPath)));
        File.WriteAllText(choices.StorePath, "{\"schemaVersion\":99,\"choices\":[]}"); var future = File.ReadAllText(choices.StorePath);
        Reject("future choice format protected", () => choices.Load());
        Check("future choice bytes untouched until explicit reset", File.ReadAllText(choices.StorePath) == future);
        choices.Reset();
        File.WriteAllText(choices.StorePath, "{\"schemaVersion\":1,\"choices\":[null]}");
        Reject("null choice record fails closed", () => choices.Load());
        choices.Reset();
        Reject("invalid append preserves valid choice store", () => choices.Append(new(Guid.Empty, DateTimeOffset.UtcNow, "da-DK", "test", "id", "noun", "sb", new string('A',64))));
        Check("invalid append did not replace data", choices.Load().Count == 0);
        if (interactive)
        {
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("/WordSuggestorWindows.App;component/SharedStyles.xaml", UriKind.Relative) });
            var main = new MainWindow(vm); SettingsWindow? settingsWindow = null;
            var foreground = GetForegroundWindow(); GetCursorPos(out var cursor);
            try
            {
                snapshot = store.Load(); snapshot.IsTextAnalyzerEnabled = true; snapshot.IsPunctuationDiagnosticsEnabled = true; store.Save(snapshot);
                main.Show(); main.OpenEditor(); await Task.Delay(200);
                var editorWindow = Field<InternalEditorWindow>(main, "_editorWindow");
                RichTextBox Editor() => (RichTextBox)typeof(MainWindow).GetProperty("EditorTextBox", Private)!.GetValue(main)!;
                vm.CaretIndex = 11; vm.EditorText = "test,verden"; Editor().CaretPosition = Editor().Document.ContentEnd;
                await Until(() => vm.AnalysisDocument is not null && Field<object?>(main, "_analysisAdorner") is not null);
                Check("native analysis paints without moving caret/selection", Editor().Selection.IsEmpty && vm.EditorText == "test,verden" && Editor().IsKeyboardFocusWithin);
                var point = (TextPointer)typeof(MainWindow).GetMethod("GetTextPointerAtCharOffset", Private)!.Invoke(main, [4])!;
                var rectangle = point.GetCharacterRect(LogicalDirection.Forward); var screen = Editor().PointToScreen(new Point(rectangle.Left + 2, rectangle.Top + rectangle.Height / 2));
                SetCursorPos((int)screen.X, (int)screen.Y); mouse_event(8, 0, 0, 0, UIntPtr.Zero); mouse_event(16, 0, 0, 0, UIntPtr.Zero); await Task.Delay(200);
                var menu = Editor().ContextMenu;
                Check("native right click exposes Core correction", menu is { IsOpen: true } && menu.Items.OfType<MenuItem>().Any(i => i.IsEnabled && i.Header?.ToString()?.StartsWith(SettingsText.Get("ApplyCorrection")) == true));
                var apply = menu!.Items.OfType<MenuItem>().First(i => i.IsEnabled && i.Header?.ToString()?.StartsWith(SettingsText.Get("ApplyCorrection")) == true);
                await Click(apply);
                Console.WriteLine("DIAGNOSTIC analysis route=" + Field<string>(main, "_analysisCommitRoute"));
                await Until(() => vm.EditorText == "test, verden");
                Check("mouse correction keeps editor focus", Editor().IsKeyboardFocusWithin);
                Chord(new WindowInteropHelper(editorWindow).Handle, 0x11, 0x5A); await Until(() => vm.EditorText == "test,verden");
                Check("whole correction is one native undo", vm.EditorText == "test,verden");
                await Until(() => vm.AnalysisDocument is not null);
                snapshot = store.Load(); snapshot.SaveWordChoices = true; store.Save(snapshot);
                point = (TextPointer)typeof(MainWindow).GetMethod("GetTextPointerAtCharOffset", Private)!.Invoke(main, [1])!;
                rectangle = point.GetCharacterRect(LogicalDirection.Forward); screen = Editor().PointToScreen(new Point(rectangle.Left + 2, rectangle.Top + rectangle.Height / 2));
                SetCursorPos((int)screen.X, (int)screen.Y); mouse_event(8, 0, 0, 0, UIntPtr.Zero); mouse_event(16, 0, 0, 0, UIntPtr.Zero); await Task.Delay(100);
                var choose = Editor().ContextMenu!.Items.OfType<MenuItem>().Last(i => System.Windows.Automation.AutomationProperties.GetAutomationId(i) == "ChooseCoreReading");
                await Click(choose); await Until(() => vm.LoadSavedChoices().Count == 1);
                Check("native interpretation click records only explicit choice", vm.LoadSavedChoices().Single().Word == "test");
                settingsWindow = new SettingsWindow(vm); settingsWindow.Show(); settingsWindow.Activate();
                var navigation = (ListBox)settingsWindow.FindName("CategoryNavigation"); navigation.SelectedIndex = 4; await Task.Delay(300);
                Check("all analysis controls enabled with actual dependencies", ((CheckBox)settingsWindow.FindName("TextAnalyzerEnabledCheckBox")).IsEnabled && ((ComboBox)settingsWindow.FindName("CommaStyleComboBox")).IsEnabled);
                var comma = (ComboBox)settingsWindow.FindName("CommaStyleComboBox"); comma.Focus(); comma.IsDropDownOpen = true; await Task.Delay(100);
                Chord(new WindowInteropHelper(settingsWindow).Handle, 0x1B); await Task.Delay(100);
                Check("Escape closes new comma picker before Settings", !comma.IsDropDownOpen && settingsWindow.IsVisible);
                ((CheckBox)settingsWindow.FindName("TextAnalyzerEnabledCheckBox")).IsChecked = false;
                Check("analysis off disables dependent controls and clears markers", !((CheckBox)settingsWindow.FindName("PunctuationDiagnosticsCheckBox")).IsEnabled && vm.AnalysisDocument is null);
                navigation.SelectedIndex = 5; await Task.Delay(100);
                ((Button)settingsWindow.FindName("SavedChoicesButton")).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Check("saved choice detail uses same settings page with Back", ((StackPanel)settingsWindow.FindName("SavedChoicesPanel")).IsVisible && ((ItemsControl)settingsWindow.FindName("SavedChoicesList")).Items.Count == 1);
            }
            finally
            {
                settingsWindow?.Close(); vm.EditorText = "";
                if (typeof(MainWindow).GetField("_workspace", Private)?.GetValue(main) is { } workspace)
                    foreach (var tab in (IEnumerable)workspace.GetType().GetProperty("Tabs")!.GetValue(workspace)!) tab.GetType().GetMethod("UpdateText")!.Invoke(tab, [""]);
                main.Close(); if (foreground != IntPtr.Zero) SetForegroundWindow(foreground); SetCursorPos(cursor.X, cursor.Y);
            }
        }
        Console.WriteLine($"PASS ANALYSIS SETTINGS {count} checks; desktop={interactive}.");
    }
    private static async Task Until(Func<bool> condition) { var watch = Stopwatch.StartNew(); while (!condition()) { if (watch.ElapsedMilliseconds > 15000) throw new Exception("Analysis condition timed out."); await Task.Delay(20); } }
    private static T Field<T>(object obj, string name) => (T)obj.GetType().GetField(name, Private)!.GetValue(obj)!;
    private static async Task Click(FrameworkElement element) { var p = element.PointToScreen(new Point(element.ActualWidth / 2, element.ActualHeight / 2)); SetCursorPos((int)p.X, (int)p.Y); mouse_event(2, 0, 0, 0, UIntPtr.Zero); mouse_event(4, 0, 0, 0, UIntPtr.Zero); await Task.Delay(100); }
    private static void Chord(IntPtr target, params byte[] keys) { if (GetForegroundWindow() != target) throw new Exception("Input target changed."); try { foreach (var key in keys) keybd_event(key, 0, 0, UIntPtr.Zero); } finally { foreach (var key in keys.Reverse()) keybd_event(key, 0, 2, UIntPtr.Zero); } }
    [StructLayout(LayoutKind.Sequential)] private struct Cursor { public int X, Y; }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Cursor point);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
