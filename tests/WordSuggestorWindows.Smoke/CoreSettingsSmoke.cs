using System.Diagnostics;
using System.IO;
using System.Text.Json;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

internal static class CoreSettingsSmoke
{
    private const string ModeVariable = "WORDSUGGESTOR_TEST_SETTINGS_BRIDGE_MODE";
    public static bool IsBridgeChild(string[] args) => Environment.GetEnvironmentVariable(ModeVariable) is not null &&
        args.Any(a => a is "--stdio-json-v1" or "--stdio-json-v2");

    public static int FakeBridge()
    {
        var request = JsonDocument.Parse(Console.In.ReadToEnd()).RootElement;
        var mode = Environment.GetEnvironmentVariable(ModeVariable);
        var id = request.GetProperty("requestId").GetString();
        var version = request.GetProperty("version").GetInt32();
        var operation = request.GetProperty("operation").GetString();
        if (operation == "capabilities")
        {
            Console.Write(JsonSerializer.Serialize(new { version = 1, requestId = id,
                errorCode = mode == "legacy" ? "invalid_request" : null,
                capabilities = mode == "legacy" ? null : new CoreCapabilities { SchemaVersion = 1, ProtocolVersions = [1, 2], Operations = ["suggest", "analyze", "wordInfo", "domains", "domainSuggestions"] } }, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            return 0;
        }
        if (version == 1) { Console.Write(JsonSerializer.Serialize(new { version, requestId = id, suggestions = Array.Empty<object>() })); return 0; }
        if (mode == "hang")
        {
            File.WriteAllText(Environment.GetEnvironmentVariable("WORDSUGGESTOR_TEST_SETTINGS_CHILD_PID")!, Environment.ProcessId.ToString());
            Thread.Sleep(30_000);
        }
        if (mode == "exit") { Console.Error.Write("PRIVATE_SETTINGS_DOCUMENT_SENTINEL"); return 17; }
        if (mode == "malformed") { Console.Write("PRIVATE_SETTINGS_DOCUMENT_SENTINEL"); return 0; }
        var scope = request.GetProperty("context").Deserialize<CoreRequestContext>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var response = new Dictionary<string, object?>
        {
            ["version"] = mode == "version" ? 3 : 2,
            ["requestId"] = mode == "id" ? "wrong" : id,
            ["context"] = mode == "generation" ? scope with { Generation = scope.Generation + 1 } : mode == "document" ? scope with { DocumentId = Guid.NewGuid() } : scope,
            ["language"] = mode == "language" ? "de-DE" : request.GetProperty("language").GetString(),
            ["suggestions"] = mode == "missing" ? null : new[] { new { term = mode == "empty_term" ? "" : "synthetic", insertText = "synthetic", score = 1, kind = "prefix", type = "word" } },
            ["diagnostics"] = new[] { new { start = mode == "range" ? 500 : 1, length = 1, issueType = "punctuation", severity = "warning", message = "Synthetic", confidence = 1.0, edits = Array.Empty<object>(), replacements = Array.Empty<string>() } }
        };
        if (mode == "rejected") response["errorCode"] = "PRIVATE_SETTINGS_DOCUMENT_SENTINEL";
        Console.Write(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        return 0;
    }

    public static async Task Run(string repo)
    {
        var root = Path.Combine(repo, ".artifacts", "tests", "core-settings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", root);
        var count = 0;
        void Check(string title, bool ok) { if (!ok) throw new Exception(title); count++; Console.WriteLine("PASS WINDOWS CORE SETTINGS " + title); }
        async Task Reject(string title, Func<Task> action, Type? expected = null)
        {
            Exception? rejected = null;
            try { await action(); } catch (Exception ex) { rejected = ex; }
            Check(title, rejected is not null && (expected is null || expected.IsInstanceOfType(rejected)));
            Check("rejection has no document or raw stderr", !rejected!.Message.Contains("PRIVATE_SETTINGS_DOCUMENT_SENTINEL"));
        }

        var provider = new WordSuggestorCoreCliSuggestionProvider(localDataRoot: root);
        var scope = new CoreRequestContext(Guid.NewGuid(), 7);
        var caps = await provider.GetCapabilitiesAsync();
        Check("new protocol discovered over v1 handshake", caps.Supports("analyze") && caps.SourcePreferences);
        Check("real DA pack capabilities are not invented", !caps.WordFamilies && !caps.ContextReranking && !caps.DomainCatalog);
        Check("bridge cannot enable learning or sharing", !caps.ChoicePersistence && !caps.AdaptiveLearning);
        var existing = await provider.SuggestAsync("skri", default);
        var extended = await provider.SuggestWithOptionsAsync("skri", scope, new());
        Check("existing and extended Core default ordering/scores match", existing.Select(x => (x.Term, x.Score, x.Kind)).SequenceEqual(extended.Value.Select(x => (x.Term, x.Score, x.Kind))));
        Check("result belongs to caller's exact document and generation", extended.IsCurrent(scope, "da-DK"));
        Check("next generation cannot consume prior result", !extended.IsCurrent(scope with { Generation = 8 }, "da-DK"));
        Check("other document cannot consume prior result", !extended.IsCurrent(scope with { DocumentId = Guid.NewGuid() }, "da-DK"));
        Check("language switch cannot consume prior result", !extended.IsCurrent(scope, "de-DE"));
        var info = await provider.GetWordInfoAsync("skrive", scope, includeFamilies: true);
        Check("real Core morphology exposed without C# grammar", info.Value.Analyses.Any(a => a.Pos == "verb" && a.WordClass == "verb"));
        Check("missing families produce availability warning", info.Value.Families.Count == 0 && info.WarningCode == "families_unavailable");
        var again = await provider.GetWordInfoAsync("skrive", scope with { Generation = 8 });
        Check("source interpretation IDs survive a new request", info.Value.Analyses.Select(a => a.Id).SequenceEqual(again.Value.Analyses.Select(a => a.Id)));
        var text = "😀 a\u030a,verden";
        var findings = await provider.AnalyzeAsync(text, scope, new(Punctuation: true));
        Check("actual Core diagnostics use original UTF-16", findings.Value.Any(d => d.RuleId == "ws_punctuation_space_after_mark" && text.Substring(d.Start, d.Length) == "," && d.Start == 5));
        Check("Core grouped edits cross the adapter unchanged", findings.Value.Any(d => d.Edits.Any(e => e.Start == 5 && e.Replacement == ", ")));
        Check("disabled diagnostics do not create findings", (await provider.AnalyzeAsync(text, scope, new())).Value.Count == 0);
        Check("missing catalogue is a calm empty state", (await provider.GetDomainsAsync(null, scope)).Value.Count == 0);
        var catalog = Path.Combine(Directory.GetParent(repo)!.FullName, "WordSuggestorCore", "LanguagePacks", "da_DK", "data", "raw", "specialized", "catalog.json");
        Check("explicit existing catalogue capability", (await provider.GetCapabilitiesAsync(catalog)).DomainCatalog);
        Check("Core domain catalogue available", (await provider.GetDomainsAsync(catalog, scope)).Value.Any(d => d.Id == "ai_ml_llm"));
        Check("Core domain insert form retained", (await provider.SuggestDomainAsync("kunstig", catalog, "ai_ml_llm", scope)).Value.Any(c => c.InsertText.Contains("intelligens")));
        await Reject("unavailable ngram data is not simulated", async () => await provider.SuggestWithOptionsAsync("jeg sk", scope, new(ContextBased: true)), typeof(CoreBridgeRejectedException));
        await Reject("oversize input is rejected without truncating document ranges", async () => await provider.AnalyzeAsync(new string('x', 16001), scope, new()), typeof(ArgumentException));
        await Reject("empty document identity rejected", async () => await provider.GetWordInfoAsync("skrive", new(Guid.Empty, 0)), typeof(ArgumentException));

        var fakePath = Environment.ProcessPath!;
        try
        {
            Environment.SetEnvironmentVariable(ModeVariable, "legacy");
            var legacy = new WordSuggestorCoreCliSuggestionProvider(cliPath: fakePath, localDataRoot: root);
            Check("old explicit rejection negotiates legacy safely", !(await legacy.GetCapabilitiesAsync()).Supports("wordInfo"));
            Check("legacy ordinary suggestion path still works", (await legacy.SuggestAsync("skri", default)).Count == 0);
            await Reject("old Core cannot silently ignore new options", async () => await legacy.SuggestWithOptionsAsync("skri", scope, new(Phonetic: false)), typeof(NotSupportedException));
            foreach (var mode in new[] { "version", "id", "generation", "document", "language", "missing", "empty_term", "exit", "malformed", "rejected" })
            {
                Environment.SetEnvironmentVariable(ModeVariable, mode);
                await Reject("invalid extended response rejected: " + mode, async () => await legacy.SuggestWithOptionsAsync("PRIVATE_SETTINGS_DOCUMENT_SENTINEL", scope, new()));
            }
            foreach (var mode in new[] { "range", "surrogate" })
            {
                Environment.SetEnvironmentVariable(ModeVariable, mode);
                await Reject("invalid UTF-16 diagnostic range rejected: " + mode, async () => await legacy.AnalyzeAsync("😀 text", scope, new(Punctuation: true)));
            }
            var pidFile = Path.Combine(root, "child.pid");
            Environment.SetEnvironmentVariable(ModeVariable, "hang");
            Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_SETTINGS_CHILD_PID", pidFile);
            var timed = new WordSuggestorCoreCliSuggestionProvider(cliPath: fakePath, localDataRoot: root, timeout: TimeSpan.FromMilliseconds(900));
            await Reject("extended process timeout", async () => await timed.GetWordInfoAsync("skrive", scope), typeof(TimeoutException));
            Check("timed-out extended child reaped", ChildGone(pidFile));
            using var cancellation = new CancellationTokenSource(1200);
            await Reject("extended process cancellation", async () => await legacy.GetWordInfoAsync("skrive", scope, cancellationToken: cancellation.Token), typeof(OperationCanceledException));
            Check("cancelled extended child reaped", ChildGone(pidFile));
        }
        finally
        {
            Environment.SetEnvironmentVariable(ModeVariable, null);
            Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_SETTINGS_CHILD_PID", null);
        }
        Console.WriteLine($"PASS WINDOWS CORE SETTINGS {count} checks.");
    }

    private static bool ChildGone(string path)
    {
        if (!File.Exists(path)) return false;
        try { using var process = Process.GetProcessById(int.Parse(File.ReadAllText(path))); return process.HasExited; }
        catch (ArgumentException) { return true; }
    }
}
