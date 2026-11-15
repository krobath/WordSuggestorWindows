using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

internal static class Program
{
    private static int _passed;
    private static string _testRoot = "";

    [STAThread]
    private static int Main(string[] args)
    {
        if (args.FirstOrDefault() == "--installer-desktop-state") return DesktopEnvironmentProbe.PrintState();
        if (args.Contains("--tts-fake-worker")) return SpeechSettingsSmoke.FakeWorker();
        if (UnknownWordSmoke.IsChild(args)) return UnknownWordSmoke.Bridge(args);
        if (CommittedWordAssessmentSmoke.IsChild(args)) return CommittedWordAssessmentSmoke.FakeBridge();
        if (CoreSettingsSmoke.IsBridgeChild(args)) return CoreSettingsSmoke.FakeBridge();
        if (args.Contains("--stdio-json-v1")) return FakeCore();
        if (args.FirstOrDefault() == "--activate-app")
        {
            try
            {
                var pid = PackageActivation.Activate(args[1], args[2]);
                Console.WriteLine("PACKAGE ACTIVATION returned process " + pid);
                // A short-lived packaged probe can exit before COM returns its PID.
                // The caller requires a fresh identity/version-bound result file;
                // activation success alone never qualifies the installed app/Core.
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine("FAIL package activation: " + ex.GetType().Name + " 0x" + ex.HResult.ToString("X8")); return 1; }
        }
        if (args.FirstOrDefault() == "--activate-update-fixture") return args.Length == 3 ? UpdatePackageActivation.Start(args[1], args[2]) : 2;
        if (args.FirstOrDefault() == "--activate-settings-fixture") return args.Length == 3 ? UpdatePackageActivation.Start(args[1], args[2], true) : 2;
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var code = 1;
        app.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                if (args.Contains("--exe-updates")) { await ExeUpdateSmoke.Run(FindRepo()); code = 0; return; }
                else if (args.Contains("--update-native")) { Console.WriteLine("BLOCKED UPDATE NATIVE: use test_settings_platform.ps1 -PackagedUpdateProbe with an installed trusted isolated LocalTest package; no production mutation."); code = 3; return; }
                else if (args.FirstOrDefault()=="--retention-data"&&args.Length==4) await RetentionDataSmoke.Run(args[1],args[2],args[3]);
                else if (args.Contains("--settings-layout")) await SettingsLayoutSmoke.Run(FindRepo());
                else if (args.Contains("--unknown-sharing")) await UnknownWordSharingSmoke.Run(FindRepo());
                else if (args.Contains("--automatic-word")) await AutomaticWordSmoke.Run(FindRepo());
                else if (args.Contains("--unknown-word")) await UnknownWordSmoke.Run(FindRepo(), false);
                else if (args.Contains("--unknown-word-ui")) await UnknownWordSmoke.Run(FindRepo(), true);
                else if (args.Contains("--advanced-state")) await AdvancedSettingsSmoke.Run(false);
                else if (args.Contains("--advanced-ui")) await AdvancedSettingsSmoke.Run(true);
                else if (args.Contains("--update-state")) await UpdateSettingsSmoke.Run(false);
                else if (args.Contains("--update-ui")) await UpdateSettingsSmoke.Run(true);
                else if (args.Contains("--hello-native")) { if (!await AccountHelloNativeSmoke.Run()) { code = 3; return; } }
                else if (args.Contains("--account-state")) await AccountSettingsSmoke.Run(false);
                else if (args.Contains("--account-ui")) await AccountSettingsSmoke.Run(true);
                else if (args.Contains("--analysis-state")) await AnalysisSettingsSmoke.Run(FindRepo(), false);
                else if (args.Contains("--analysis-ui")) await AnalysisSettingsSmoke.Run(FindRepo(), true);
                else if (args.Contains("--suggestion-state")) await SuggestionSettingsSmoke.Run(FindRepo(), false);
                else if (args.Contains("--suggestion-settings")) await SuggestionSettingsSmoke.Run(FindRepo());
                else if (args.Contains("--speech-ui")) await SpeechUiSmoke.Run(FindRepo());
                else if (args.Contains("--speech-settings")) await SpeechSettingsSmoke.Run(FindRepo());
                else if (args.Contains("--core-settings")) await CoreSettingsSmoke.Run(FindRepo());
                else if (args.Contains("--committed-word")) await CommittedWordAssessmentSmoke.Run(FindRepo());
                else if (args.Contains("--settings-state")) SettingsStateSmoke.Run(Path.Combine(FindRepo(), ".artifacts", "tests", "settings-state-" + Guid.NewGuid().ToString("N")));
                else if (args.Contains("--settings-ui")) await SettingsUiSmoke.Run(FindRepo());
                else if (args.Contains("--gui") || args.Contains("--tabs-gui")) await GuiSmoke.Run(FindRepo(), args.Contains("--tabs-gui"));
                else if (args.Contains("--media")) await MediaSmoke.Run(FindRepo());
                else await RunTests();
                code = 0;
            }
            catch (Exception ex) { Console.Error.WriteLine($"FAIL {ex}"); }
            finally { app.Shutdown(); }
        });
        app.Run();
        return code;
    }

    private static int FakeCore()
    {
        var request = JsonDocument.Parse(Console.In.ReadToEnd()).RootElement;
        var mode = Environment.GetEnvironmentVariable("WORDSUGGESTOR_TEST_CORE_MODE");
        if (mode == "hang")
        {
            File.WriteAllText(Environment.GetEnvironmentVariable("WORDSUGGESTOR_TEST_CHILD_PID")!, Environment.ProcessId.ToString());
            Thread.Sleep(30_000);
        }
        if (mode == "exit") { Console.Error.WriteLine("PRIVATE_DOCUMENT_MUST_NOT_ESCAPE"); return 17; }
        if (mode == "malformed") { Console.Write("{}"); return 0; }
        Console.Write(JsonSerializer.Serialize(new { version = mode == "version" ? 2 : 1,
            requestId = mode == "id" ? "wrong" : request.GetProperty("requestId").GetString(), suggestions = Array.Empty<object>() }));
        return 0;
    }

    private static async Task RunTests()
    {
        _passed += DesktopEnvironmentSmoke.Run();
        _passed += await UpdateSmoke.Run();
        var repo = FindRepo();
        _testRoot = Path.Combine(repo, ".artifacts", "tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", _testRoot);
        var probeResult = Path.Combine(_testRoot, "diagnostics", "qualification-" + Guid.NewGuid().ToString("N") + ".json");
        Check("qualification result uses reserved diagnostics name", ReleaseQualificationProbe.IsReservedResultPath(_testRoot, probeResult));
        Check("qualification cannot overwrite settings or documents", !ReleaseQualificationProbe.IsReservedResultPath(_testRoot, Path.Combine(_testRoot, "settings", "settings-v1.json")) &&
            !ReleaseQualificationProbe.IsReservedResultPath(_testRoot, Path.Combine(_testRoot, "diagnostics", "notes.txt")));
        Check("qualification cannot escape its data root", !ReleaseQualificationProbe.IsReservedResultPath(_testRoot, Path.Combine(_testRoot, "..", "diagnostics", Path.GetFileName(probeResult))));
        Directory.CreateDirectory(Path.GetDirectoryName(probeResult)!);
        File.WriteAllText(probeResult, "existing");
        Check("qualification cannot overwrite an existing result", !ReleaseQualificationProbe.IsReservedResultPath(_testRoot, probeResult));
        _passed += EditorTabsSmoke.Run(_testRoot);
        var pack = Path.Combine(Directory.GetParent(repo)!.FullName, "WordSuggestorCore", "Ressources", "da_lexicon.sqlite");
        Check("legacy Danish pack", LanguagePackValidator.Validate(pack, "da_DK", true) is { Compatible: true, Legacy: true });
        Check("legacy requires explicit compatibility", !LanguagePackValidator.Validate(pack, "da_DK").Compatible);
        Check("legacy cannot masquerade as English", !LanguagePackValidator.Validate(pack, "en_US", true).Compatible);
        var corrupt = Path.Combine(_testRoot, "corrupt.sqlite");
        File.WriteAllText(corrupt, new string('x', 300));
        Check("corrupt pack rejected", !LanguagePackValidator.Validate(corrupt, "da_DK").Compatible);
        Check("missing pack rejected", !LanguagePackValidator.Validate(corrupt + ".absent", "da_DK").Compatible);
        var modern = Path.Combine(_testRoot, "modern.sqlite");
        PackFixture.Create(modern, "da_DK");
        Check("modern metadata accepted", LanguagePackValidator.Validate(modern, "da_DK") is { Compatible: true, Legacy: false, Version: "1.0.0001" });
        Check("wrong modern language rejected", LanguagePackValidator.Validate(modern, "de_DE").Code == "wrong_language");
        var future = Path.Combine(_testRoot, "future.sqlite");
        PackFixture.Create(future, "da_DK", 15);
        Check("future pack schema rejected", LanguagePackValidator.Validate(future, "da_DK").Code == "incompatible_schema");
        var incomplete = Path.Combine(_testRoot, "incomplete.sqlite");
        PackFixture.Create(incomplete, "da_DK", missingColumn: true);
        Check("missing parity column rejected", LanguagePackValidator.Validate(incomplete, "da_DK").Code == "incompatible_schema");
        var invalidVersion = Path.Combine(_testRoot, "version.sqlite");
        PackFixture.Create(invalidVersion, "da_DK", major: "2");
        Check("inconsistent pack version rejected", LanguagePackValidator.Validate(invalidVersion, "da_DK").Code == "invalid_version");
        Check("Unicode prefix replacement", EditorCommit.Replace("Hej æø skri", 11, 11, "skrive") == ("Hej æø skrive ", 14));
        Check("middle token and suffix", EditorCommit.Replace("Jeg skriver igen", 8, 8, "skrive") == ("Jeg skrive igen", 10));
        Check("paragraph and emoji offsets", EditorCommit.Replace("😀\r\nsmø", 7, 7, "smør") == ("😀\r\nsmør ", 9));
        Check("selection replacement", EditorCommit.Replace("en gammel bog", 3, 9, "ny") == ("en ny bog", 5));
        Check("tab boundary preserved", EditorCommit.Replace("skri\tigen", 4, 4, "skrive") == ("skrive\tigen", 6));
        Check("secondary negative-origin monitor", ScreenPlacement.Compute(new Rect(-1920, 0, 1920, 1080), new Point(-1900, 1000), new Size(438, 798), true) == new Point(-1912, 184));
        Check("oversized overlay remains reachable", ScreenPlacement.Compute(new Rect(0, 0, 640, 480), new Point(2000, -100), new Size(900, 900), false) == new Point(8, 8));

        var settings = new WindowsAppSettingsService();
        var snapshot = settings.Load();
        snapshot.SelectedLanguageCode = "de-DE";
        settings.Save(snapshot);
        Check("settings restart persistence", new WindowsAppSettingsService().Load().SelectedLanguageCode == "de-DE");
        var insights = new WindowsErrorInsightsStore();
        insights.RecordAcceptedSuggestion("PRIVATE_PREFIX", new("PRIVATE_WORD", 1, "prefix", "word", null, null), "da-DK", 1);
        var stored = File.ReadAllText(insights.StorePath);
        Check("insights private by default", !stored.Contains("PRIVATE_PREFIX") && !stored.Contains("PRIVATE_WORD"));
        File.WriteAllText(settings.SettingsPath, "{bad json");
        Check("malformed settings recover", settings.Load().SelectedLanguageCode == "da-DK");
        File.WriteAllText(settings.SettingsPath, "{\"schemaVersion\":99,\"futureKey\":true}");
        await Throws<InvalidOperationException>("rollback preserves newer settings", () => Task.Run(() => settings.Save(new())));
        Check("newer settings unchanged", File.ReadAllText(settings.SettingsPath).Contains("futureKey"));
        File.WriteAllText(settings.SettingsPath, "{}");
        Check("diagnostic detail excluded", !new SelectionImportDiagnostic(DateTimeOffset.Now, "NativeEdit", "Rejected", "PRIVATE_DOCUMENT").ToString().Contains("PRIVATE_DOCUMENT"));

        var provider = new WordSuggestorCoreCliSuggestionProvider(localDataRoot: _testRoot);
        Check("Danish installed", provider.SelectedLanguage.IsPackAvailable);
        Check("empty English rejected", !provider.LanguageOptions.Single(l => l.LanguageCode == "en-US").IsPackAvailable);
        foreach (var (prefix, expected) in new[] { ("skri", "skrive"), ("smør", "smør"), ("blå", "blå"), ("ø", "ønske"), ("læ", "lægge") })
        {
            var watch = Stopwatch.StartNew();
            var suggestions = await provider.SuggestAsync(prefix, default);
            Check($"real Core fixture {prefix} ({watch.ElapsedMilliseconds} ms)", suggestions.Count > 0 && suggestions.Any(s => s.Term == expected));
        }
        await provider.AddUserWordAsync("betasolsikke");
        var restartedProvider = new WordSuggestorCoreCliSuggestionProvider(localDataRoot: _testRoot);
        Check("Core user words persist after restart", (await restartedProvider.SuggestAsync("betasols", default)).Any(s => s.Term == "betasolsikke"));
        var userFile = Path.Combine(_testRoot, "UserWords", "local_lexicon_overlay_v2.json");
        var userData = File.ReadAllText(userFile);
        File.WriteAllText(userFile, "{\"schema_version\":\"future_v99\",\"entries_by_language\":{}}");
        Check("future user words do not stop base suggestions", (await restartedProvider.SuggestAsync("skri", default)).Any(s => s.Term == "skrive") && restartedProvider.HealthWarning is not null);
        await Throws<InvalidOperationException>("future user words protected from writes", () => restartedProvider.AddUserWordAsync("testord"));
        Check("future user words preserved", File.ReadAllText(userFile).Contains("future_v99"));
        File.WriteAllText(userFile, userData);
        var unavailable = new WordSuggestorCoreCliSuggestionProvider(cliPath: Path.Combine(_testRoot, "missing.exe"));
        await Throws<FileNotFoundException>("unavailable Core", () => unavailable.SuggestAsync("skri", default));

        var fakeExe = Environment.ProcessPath!;
        foreach (var mode in new[] { "exit", "malformed", "version", "id" })
        {
            Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_CORE_MODE", mode);
            var fake = new WordSuggestorCoreCliSuggestionProvider(cliPath: fakeExe, localDataRoot: _testRoot);
            await Throws<Exception>($"reject Core {mode}", async () =>
            {
                try { await fake.SuggestAsync("PRIVATE_DOCUMENT_MUST_NOT_ESCAPE", default); }
                catch (Exception ex) { Check("stderr/input not exposed", !ex.Message.Contains("PRIVATE_DOCUMENT")); throw; }
            });
        }
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_CORE_MODE", "hang");
        var pidFile = Path.Combine(_testRoot, "child.pid");
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_CHILD_PID", pidFile);
        var timed = new WordSuggestorCoreCliSuggestionProvider(cliPath: fakeExe, localDataRoot: _testRoot, timeout: TimeSpan.FromMilliseconds(800));
        await Throws<TimeoutException>("Core timeout", () => timed.SuggestAsync("skri", default));
        Check("timed out process reaped", ChildGone(pidFile));
        using (var cancellation = new CancellationTokenSource(800))
            await Throws<OperationCanceledException>("Core cancellation", () => timed.SuggestAsync("skri", cancellation.Token));
        Check("cancelled process reaped", ChildGone(pidFile));
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_CORE_MODE", null);

        var controlled = new ControlledProvider(provider.SelectedLanguage);
        using var vm = new MainWindowViewModel(controlled, insights, settings, "skri");
        vm.CanCommitLocally = () => true;
        vm.EnsureEditorExpanded();
        vm.SetLocalEditorActive(true);
        await Until(() => controlled.Requests.Count == 1);
        vm.CaretIndex = 3;
        vm.EditorText = "smø";
        await Until(() => controlled.Requests.Count == 2);
        controlled.Requests[1].SetResult([new("smør", 1, "prefix", "word", null, null)]);
        await Until(() => vm.Suggestions.Count == 1);
        controlled.Requests[0].SetResult([new("stale", 1, "prefix", "word", null, null)]);
        await Task.Delay(150);
        Check("out-of-order result ignored", vm.Suggestions.Single().Term == "smør");
        Check("shared candidate accept", vm.AcceptSuggestionAtIndex(0) && vm.EditorText == "smør " && vm.CaretIndex == 5);
        Check("duplicate accept rejected", !vm.AcceptSuggestionAtIndex(0));
        Check("accepted list cleared", vm.Suggestions.Count == 0 && vm.ShouldShowSuggestionOverlay);
        vm.SetLocalEditorActive(false);
        Check("defocused editor hides overlay", !vm.ShouldShowSuggestionOverlay);
        vm.UpdateExternalSuggestionToken("skri", new IntPtr(123), "test");
        await Until(() => controlled.Requests.Count == 3);
        controlled.Requests[2].SetResult([new("skrive", 1, "prefix", "word", null, null)]);
        await Until(() => vm.Suggestions.Count == 1);
        var commits = 0;
        vm.ExternalCommitRequested = request => { commits++; return request.WindowHandle == new IntPtr(123); };
        var localBefore = vm.EditorText;
        Check("mouse/external uses same route", vm.AcceptSuggestion(vm.Suggestions[0]) && commits == 1 && vm.EditorText == localBefore);
        Check("external duplicate rejected", !vm.AcceptSuggestionAtIndex(0));
        var tabProvider = new ControlledProvider(provider.SelectedLanguage);
        using var tabVm = new MainWindowViewModel(tabProvider, insights, settings, "skri");
        tabVm.CanCommitLocally = () => true;
        tabVm.EnsureEditorExpanded(); tabVm.SetLocalEditorActive(true);
        await Until(() => tabProvider.Requests.Count == 1);
        tabVm.SwitchEditorDocument("skri", 4, 4, 4);
        await Until(() => tabProvider.Requests.Count == 2);
        tabProvider.Requests[0].SetResult([new("stale-tab", 1, "prefix", "word", null, null)]);
        await Task.Delay(100);
        Check("same-text tab switch rejects old Core completion", tabVm.Suggestions.Count == 0);
        tabProvider.Requests[1].SetResult([new("skrive", 1, "prefix", "word", null, null)]);
        await Until(() => tabVm.Suggestions.Count == 1);
        tabVm.SwitchEditorDocument("", 0, 0, 0);
        Check("tab switch clears candidates before commit", !tabVm.AcceptSuggestionAtIndex(0) && tabVm.EditorText == "" && !tabVm.ShouldShowSuggestionOverlay);
        Console.WriteLine($"PASS {_passed} checks. Test data: .artifacts/tests/{Path.GetFileName(_testRoot)}");
    }

    private static bool ChildGone(string path)
    {
        if (!File.Exists(path)) return true;
        try { return Process.GetProcessById(int.Parse(File.ReadAllText(path))).HasExited; }
        catch (ArgumentException) { return true; }
    }
    private static void Check(string name, bool condition)
    {
        if (!condition) throw new Exception(name);
        _passed++; Console.WriteLine($"PASS {name}");
    }
    private static async Task Throws<T>(string name, Func<Task> action) where T : Exception
    {
        try { await action(); }
        catch (T) { Check(name, true); return; }
        throw new Exception($"{name}: expected {typeof(T).Name}");
    }
    private static async Task Until(Func<bool> predicate)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!predicate()) { if (DateTime.UtcNow > deadline) throw new TimeoutException("test condition"); await Task.Delay(20); }
    }
    private static string FindRepo()
    {
        for (var p = new DirectoryInfo(AppContext.BaseDirectory); p is not null; p = p.Parent)
            if (File.Exists(Path.Combine(p.FullName, "WordSuggestorWindows.sln"))) return p.FullName;
        throw new DirectoryNotFoundException();
    }

    private sealed class ControlledProvider(LanguageOption language) : ISuggestionProvider
    {
        public string ProviderDescription => "Test";
        public IReadOnlyList<LanguageOption> LanguageOptions => [language];
        public LanguageOption SelectedLanguage => language;
        public List<TaskCompletionSource<IReadOnlyList<SuggestionItem>>> Requests { get; } = [];
        public void SetLanguage(LanguageOption option) { }
        public Task<IReadOnlyList<SuggestionItem>> SuggestAsync(string text, CancellationToken token)
        {
            var request = new TaskCompletionSource<IReadOnlyList<SuggestionItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (Requests) Requests.Add(request);
            return request.Task; // intentionally ignores cancellation to simulate a late host
        }
    }
}
