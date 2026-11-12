using System.Diagnostics;
using System.IO;
using System.Text.Json;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

internal static class CommittedWordAssessmentSmoke
{
    private const string Mode = "WORDSUGGESTOR_TEST_COMMITTED_WORD_MODE";
    private const string PidFile = "WORDSUGGESTOR_TEST_COMMITTED_WORD_PID";
    private const string PrivateWord = "zzzsensitivkandidat";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static bool IsChild(string[] args) => Environment.GetEnvironmentVariable(Mode) is not null && args.Any(a => a is "--stdio-json-v1" or "--stdio-json-v2");

    public static int FakeBridge()
    {
        using var input = JsonDocument.Parse(Console.In.ReadToEnd());
        var request = input.RootElement;
        var mode = Environment.GetEnvironmentVariable(Mode);
        var id = request.GetProperty("requestId").GetString();
        if (request.GetProperty("operation").GetString() == "capabilities")
        {
            Console.Write(JsonSerializer.Serialize(new { version = 1, requestId = id,
                errorCode = mode == "legacy" ? "invalid_request" : null,
                capabilities = new CoreCapabilities { SchemaVersion = 1, ProtocolVersions = [1, 2],
                    Operations = mode == "no_operation" ? [] : ["assessCommittedWord"], CommittedWordAssessment = mode != "no_capability" } }, Json));
            return 0;
        }
        var pid = Environment.GetEnvironmentVariable(PidFile);
        if (pid is not null) File.WriteAllText(pid, Environment.ProcessId.ToString());
        if (mode == "hang") Thread.Sleep(30_000);
        if (mode == "exit") { Console.Error.Write(PrivateWord); return 19; }
        if (mode == "malformed") { Console.Write(PrivateWord); return 0; }
        var context = request.GetProperty("context").Deserialize<CoreRequestContext>(Json)!;
        var result = new Dictionary<string, object?> { ["schemaVersion"] = mode == "schema" ? 99 : 1,
            ["disposition"] = mode == "disposition" ? "learned" : "candidate" };
        if (mode == "missing_schema") result.Remove("schemaVersion");
        if (mode == "missing_disposition") result.Remove("disposition");
        if (mode == "null_disposition") result["disposition"] = null;
        if (mode == "content_field") result["word"] = PrivateWord;
        var response = new Dictionary<string, object?> { ["version"] = mode == "version" ? 3 : 2,
            ["requestId"] = mode == "id" ? "wrong" : id,
            ["context"] = mode == "document" ? context with { DocumentId = Guid.NewGuid() } : mode == "generation" ? context with { Generation = context.Generation + 1 } : context,
            ["language"] = mode == "language" ? "de-DE" : request.GetProperty("language").GetString(),
            ["committedWord"] = mode == "missing_payload" ? null : result };
        if (mode == "root_content") response["text"] = PrivateWord;
        if (mode == "unrelated_payload") response["suggestions"] = Array.Empty<object>();
        if (mode == "rejected") response["errorCode"] = PrivateWord;
        var output = JsonSerializer.Serialize(response, Json);
        if (mode == "duplicate") output = output.Replace("\"disposition\":\"candidate\"", "\"disposition\":\"candidate\",\"disposition\":\"candidate\"");
        Console.Write(output);
        return 0;
    }

    public static async Task Run(string repo)
    {
        var root = Path.Combine(repo, ".artifacts", "tests", "committed-word-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var count = 0;
        void Check(string title, bool ok) { if (!ok) throw new Exception(title); count++; Console.WriteLine("PASS COMMITTED WORD " + title); }
        async Task Reject(string title, Func<Task> action, Type? expected = null)
        {
            Exception? rejection = null;
            try { await action(); } catch (Exception ex) { rejection = ex; }
            Check(title, rejection is not null && (expected is null || expected.IsInstanceOfType(rejection)));
            Check("rejection excludes word and raw process stderr", !rejection!.Message.Contains(PrivateWord, StringComparison.Ordinal));
        }
        var scope = new CoreRequestContext(Guid.NewGuid(), 10);
        var provider = new WordSuggestorCoreCliSuggestionProvider(localDataRoot: root);
        var caps = await provider.GetCapabilitiesAsync();
        Check("real assessment capability is separate from lookup and sharing", caps.Supports("assessCommittedWord") && caps.CommittedWordAssessment && caps.UnknownWordLookup && caps.ExplicitUnknownWordImport && !caps.AutomaticUnknownWordObservation && !caps.UnknownWordSharing);
        var candidate = await provider.AssessCommittedWordAsync(PrivateWord, scope);
        Check("real Core observes unknown candidate without returning content", candidate.Value is { SchemaVersion: 1, IsCandidate: true } && !candidate.Value.ToString().Contains(PrivateWord) &&
            JsonSerializer.SerializeToElement(candidate.Value, Json).EnumerateObject().Select(p => p.Name).Order().SequenceEqual(new[] { "disposition", "schemaVersion" }));
        Check("known word and short word are not candidates", !(await provider.AssessCommittedWordAsync("SKRIVE", scope)).Value.IsCandidate && !(await provider.AssessCommittedWordAsync("qx", scope)).Value.IsCandidate);
        Check("assessment belongs to exact document generation and language", candidate.IsCurrent(scope, "da-DK") && !candidate.IsCurrent(scope with { DocumentId = Guid.NewGuid() }, "da-DK") && !candidate.IsCurrent(scope with { Generation = 11 }, "da-DK") && !candidate.IsCurrent(scope, "de-DE"));
        Check("repeated assessment is idempotent", (await provider.AssessCommittedWordAsync(PrivateWord, scope)).Value == candidate.Value);
        foreach (var surface in new[] { "", "two words", "word\n", new string('x', 129) })
            await Reject("client rejects multiword or oversize request before transport", async () => await provider.AssessCommittedWordAsync(surface, scope), typeof(ArgumentException));
        await Reject("Core rejects nonlexical surface", async () => await provider.AssessCommittedWordAsync("😀", scope), typeof(CoreBridgeRejectedException));
        await Reject("assessment rejects invalid document scope", async () => await provider.AssessCommittedWordAsync(PrivateWord, new(Guid.Empty, 0)), typeof(ArgumentException));
        Check("assessment does not create a user-word directory", !Directory.Exists(Path.Combine(root, "UserWords")));
        var overlayPath = Path.Combine(root, "UserWords", "local_lexicon_overlay_v2.json");
        Directory.CreateDirectory(Path.GetDirectoryName(overlayPath)!);
        File.WriteAllText(overlayPath, "{\"schema_version\":\"local_lexicon_overlay_v2\",\"entries_by_language\":{}}"); // Prevent the existing mutable baseline from consulting global legacy data.
        var before = await provider.SuggestAsync("skri", default);
        await provider.AddUserWordAsync(PrivateWord);
        var bytes = File.ReadAllBytes(overlayPath);
        Check("explicit user word is known to Core assessment", !(await provider.AssessCommittedWordAsync(PrivateWord, scope)).Value.IsCandidate);
        Check("assessment leaves overlay bytes unchanged", File.ReadAllBytes(overlayPath).SequenceEqual(bytes));
        File.WriteAllText(overlayPath, "{\"schema_version\":\"local_lexicon_overlay_v99\",\"entries_by_language\":{}}");
        var future = File.ReadAllBytes(overlayPath);
        await Reject("future overlay fails closed without inventing an unknown word", async () => await provider.AssessCommittedWordAsync(PrivateWord, scope), typeof(CoreBridgeRejectedException));
        Check("future overlay remains unchanged", File.ReadAllBytes(overlayPath).SequenceEqual(future));
        File.WriteAllBytes(overlayPath, bytes);
        // Compare before explicit add: the assessed word is unrelated to this prefix.
        Check("assessment does not change ordered real suggestions", (await provider.SuggestAsync("skri", default)).Select(s => (s.Term,s.Score,s.Kind)).SequenceEqual(before.Select(s => (s.Term,s.Score,s.Kind))));
        using (var diagnostics = new WindowsDiagnostics(Path.Combine(root, "diagnostics")))
        {
            diagnostics.Apply(new() { IsCoreDebugLoggingEnabled = true }); provider.Diagnostics = diagnostics;
            await provider.AssessCommittedWordAsync(PrivateWord, scope); await diagnostics.FlushAsync(); provider.Diagnostics = null;
            var logs = string.Join("", Directory.GetFiles(Path.Combine(root,"diagnostics")).Select(File.ReadAllText));
            Check("assessment records safe operational metadata only", logs.Contains("AssessCommittedWord") && logs.Contains("CoreSummary") && !logs.Contains(PrivateWord) && !logs.Contains(root));
        }

        var pidPath = Path.Combine(root, "bridge.pid");
        var originalMode = Environment.GetEnvironmentVariable(Mode); var originalPid = Environment.GetEnvironmentVariable(PidFile);
        try
        {
            Environment.SetEnvironmentVariable(PidFile, pidPath);
            foreach (var mode in new[] { "legacy", "no_capability", "no_operation" })
            {
                Environment.SetEnvironmentVariable(Mode, mode);
                var old = new WordSuggestorCoreCliSuggestionProvider(cliPath: Environment.ProcessPath!, localDataRoot: root);
                await Reject("unnegotiated assessment is never sent: " + mode, async () => await old.AssessCommittedWordAsync(PrivateWord, scope), typeof(NotSupportedException));
                Check("capability failure sends no word operation", !File.Exists(pidPath));
            }
            foreach (var mode in new[] { "missing_payload", "missing_schema", "missing_disposition", "null_disposition", "schema", "disposition", "content_field", "root_content", "unrelated_payload", "duplicate", "id", "version", "document", "generation", "language", "rejected", "exit", "malformed" })
            {
                Environment.SetEnvironmentVariable(Mode, mode);
                var fake = new WordSuggestorCoreCliSuggestionProvider(cliPath: Environment.ProcessPath!, localDataRoot: root);
                await Reject("invalid assessment rejected: " + mode, async () => await fake.AssessCommittedWordAsync(PrivateWord, scope));
            }
            Environment.SetEnvironmentVariable(Mode, "hang");
            File.Delete(pidPath);
            var timed = new WordSuggestorCoreCliSuggestionProvider(cliPath: Environment.ProcessPath!, localDataRoot: root, timeout: TimeSpan.FromMilliseconds(900));
            await timed.GetCapabilitiesAsync();
            await Reject("assessment deadline reaps process", async () => await timed.AssessCommittedWordAsync(PrivateWord, scope), typeof(TimeoutException));
            Check("timed-out assessment process is gone", ChildGone(pidPath));
            File.Delete(pidPath);
            var cancellable = new WordSuggestorCoreCliSuggestionProvider(cliPath: Environment.ProcessPath!, localDataRoot: root);
            using var cancel = new CancellationTokenSource();
            var running = cancellable.AssessCommittedWordAsync(PrivateWord, scope, cancel.Token);
            await Until(() => File.Exists(pidPath));
            var startedPid = File.ReadAllText(pidPath);
            using var waiting = new CancellationTokenSource(200);
            await Reject("queued assessment can be cancelled without another process", async () => await cancellable.AssessCommittedWordAsync("andetord", scope, waiting.Token), typeof(OperationCanceledException));
            cancel.Cancel();
            await Reject("in-flight assessment cancellation is preserved", async () => await running, typeof(OperationCanceledException));
            Check("cancelled process is gone and queued request started no child", ChildGone(pidPath) && File.ReadAllText(pidPath) == startedPid);
        }
        finally { Environment.SetEnvironmentVariable(Mode, originalMode); Environment.SetEnvironmentVariable(PidFile, originalPid); }
        Check("no observation store or handoff queue created", !Directory.Exists(Path.Combine(root,"UnknownWords")) && !Directory.Exists(Path.Combine(root,"learning")));
        Console.WriteLine($"PASS COMMITTED WORD {count} checks; read-only assessment, no implicit lookup/sharing.");
    }
    private static bool ChildGone(string path)
    {
        if (!File.Exists(path)) return false;
        try { using var process = Process.GetProcessById(int.Parse(File.ReadAllText(path))); return process.HasExited; } catch (ArgumentException) { return true; }
    }
    private static async Task Until(Func<bool> condition)
    { var timer = Stopwatch.StartNew(); while (!condition()) { if (timer.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Synthetic assessment process did not start."); await Task.Delay(20); } }
}
