using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

internal static class SpeechSettingsSmoke
{
    public static int FakeWorker()
    {
        var payload = JsonDocument.Parse(Console.In.ReadToEnd()).RootElement;
        if (Environment.GetEnvironmentVariable("WORDSUGGESTOR_TEST_SPEECH_DIRECTORY") is { } report)
            File.WriteAllText(report, Path.GetDirectoryName(payload.GetProperty("wavePath").GetString())!);
        var mode = Environment.GetEnvironmentVariable("WORDSUGGESTOR_TEST_SPEECH_MODE");
        if (mode == "hang") { Console.WriteLine("WS_TTS|PLAYBACK_START"); Console.Out.Flush(); Thread.Sleep(30000); return 0; }
        if (mode == "stderr") { Console.Error.WriteLine("PRIVATE_SPEECH_TEXT"); return 7; }
        if (mode == "flood") { Console.Error.Write(new string('x', 300000)); Console.Error.Flush(); Thread.Sleep(30000); return 0; }
        var text = payload.GetProperty("text").GetString()!;
        File.WriteAllText(payload.GetProperty("wavePath").GetString()!, "synthetic wave");
        if (mode == "invalid") Console.WriteLine("WS_TTS|CUE|0|1|1|1\nWS_TTS|CUE|-1|1|0|1\nWS_TTS|CUE|0|1|0|99999");
        else Console.WriteLine("WS_TTS|CUE|0|20|0|" + text.Length);
        Console.WriteLine("WS_TTS|AUDIO|100\nWS_TTS|PLAYBACK_START"); Console.Out.Flush();
        if (mode == "slow") Thread.Sleep(30000);
        return 0;
    }
    public static async Task Run(string repo)
    {
        var root = Path.Combine(repo, ".artifacts", "tests", "speech-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root); Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", root);
        var passed = 0;
        void Check(string title, bool ok) { if (!ok) throw new Exception(title); passed++; Console.WriteLine("PASS SETTINGS SPEECH " + title); }
        for (var percent = 50; percent <= 150; percent += 5)
            Check("requested speed roundtrip " + percent, SpeechSpeedPolicy.Percentage((percent - 100) / 100.0) == percent);
        Check("speed clamps and half steps match reference", SpeechSpeedPolicy.Percentage(100) == 150 && SpeechSpeedPolicy.Percentage(-100) == 50 && SpeechSpeedPolicy.Percentage(.025) == 105 && SpeechSpeedPolicy.Percentage(-.025) == 95);
        Check("nonfinite speed has safe neutral presentation", SpeechSpeedPolicy.Normalize(double.NaN) == 0);
        var legacy = new AppSettingsSnapshot { SchemaVersion = 2, ReadingSpeedDelta = 1, ReadingStrategy = "all", UseSystemSpeechSettings = true };
        legacy.SystemVoiceIdOverrideByLanguage["da-dk"] = "saved-voice";
        var upgraded = SettingsSchema.Upgrade(legacy);
        Check("old OneCore request migrates once to nearest five percent", SpeechSpeedPolicy.Percentage(upgraded.ReadingSpeedDelta) == 135 && SettingsSchema.Upgrade(upgraded).ReadingSpeedDelta == upgraded.ReadingSpeedDelta);
        Check("old page/all strategies become real paragraph strategy", upgraded.ReadingStrategy == "paragraph" && TypingReadAloudPolicy.NormalizeStrategy("page") == "paragraph");
        Check("migration preserves explicit system and voice intentions", upgraded.UseSystemSpeechSettings && upgraded.SystemVoiceIdOverrideByLanguage["da-dk"] == "saved-voice");
        var path = Path.Combine(root, "migration.json");
        const string old = "{\"schemaVersion\":2,\"readingSpeedDelta\":-1,\"readingStrategy\":\"page\",\"storeSentenceExamples\":false,\"learningConsent\":false}";
        File.WriteAllText(path, old, new System.Text.UTF8Encoding(true)); var raw = File.ReadAllBytes(path);
        var store = new WindowsAppSettingsService(path); var loaded = store.LoadResult();
        Check("v2 migration is in-memory and read-only", loaded.State == SettingsLoadState.Migrated && raw.SequenceEqual(File.ReadAllBytes(path)));
        var edited = loaded.Settings.Clone(); edited.ReadingHighlightMode = "sentence"; store.SaveChanges(loaded.Settings, edited);
        Check("first current-schema write preserves byte-exact v2 backup", File.ReadAllBytes(path + ".v2.bak").SequenceEqual(raw) && store.Load().SchemaVersion == SettingsSchema.CurrentVersion);
        Check("migration keeps false consent and data", !store.Load().AdditionalProperties["learningConsent"].GetBoolean() && !store.Load().StoreSentenceExamples);
        var tracker = new SpeechTypingTracker(); var key = Guid.NewGuid();
        tracker.Reset(key, "æble", 4);
        Check("word strategy speaks the completed Unicode word", tracker.Observe(key, "æble ", 5, "word") is { Start: 0, Text: "æble" });
        Check("duplicate snapshots do not repeat", tracker.Observe(key, "æble ", 5, "word") is null);
        tracker.Reset(key, "😀 æble", 7);
        Check("word range is original UTF-16 after emoji", tracker.Observe(key, "😀 æble.", 8, "word") is { Start: 3, Length: 4 });
        tracker.Reset(key, "a\u030a", 2);
        Check("decomposed Unicode stays unchanged", tracker.Observe(key, "a\u030a ", 3, "word")?.Text == "a\u030a");
        tracker.Reset(key, "En sætning.", 11);
        Check("sentence waits for full stop then space", tracker.Observe(key, "En sætning. ", 12, "sentence") is { Text: "En sætning." });
        Check("question mark does not silently change sentence trigger", TypingReadAloudPolicy.Segment("sentence", "Hej? ", " ") is null);
        Check("paragraph requires full stop then newline", TypingReadAloudPolicy.Segment("paragraph", "Før.\r\nAndet.\r\n", "\r\n") is { Start: 6, Text: "Andet." });
        Check("paragraph does not read incomplete line", TypingReadAloudPolicy.Segment("paragraph", "Hej\r\n", "\r\n") is null);
        foreach (var boundary in new[] { " ", "\t", "\n", "\r", "\r\n", ".", "!", "?", ",", ";", ":" })
            Check("word trigger " + JsonSerializer.Serialize(boundary), TypingReadAloudPolicy.Segment("word", "hej" + boundary, boundary)?.Text == "hej");
        tracker.Reset(key, "hej", 3);
        Check("paste block is not auto read", tracker.Observe(key, "hej og hele teksten. ", "hej og hele teksten. ".Length, "word") is null);
        tracker.Reset(key, "hej", 3);
        Check("identical text in another document does not trigger", tracker.Observe(Guid.NewGuid(), "hej ", 4, "word") is null);
        tracker.Reset(key, "hej", 3);
        Check("none never starts audio", tracker.Observe(key, "hej ", 4, "none") is null);
        Check("invalid/split-surrogate ranges rejected", !WindowsTextToSpeechService.ValidRange("😀 ord", 1, 1) && !WindowsTextToSpeechService.ValidRange("hej", 2, 2));

        var options = new TtsSpeechOptions("da-DK", null, "Synthetic", "SAPI Desktop", "word", false, 0, null);
        ProcessStartInfo Factory()
        {
            var info = new ProcessStartInfo(Environment.ProcessPath!); info.ArgumentList.Add("--tts-fake-worker");
            return info;
        }
        try
        {
            var directoryReport = Path.Combine(root, "directory.txt");
            Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_SPEECH_DIRECTORY", directoryReport);
            foreach (var mode in new[] { "ok", "invalid", "stderr", "flood", "hang" })
            {
                Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_SPEECH_MODE", mode);
                using var service = new WindowsTextToSpeechService(Factory, TimeSpan.FromSeconds(2));
                var finished = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously); var cues = new ConcurrentQueue<TextToSpeechBoundaryCue>();
                service.SpeechStopped += (_, status) => finished.TrySetResult(status); service.BoundaryCueReceived += (_, cue) => cues.Enqueue(cue);
                service.Speak(mode == "invalid" ? "😀" : "PRIVATE_SPEECH_TEXT", options);
                var pid = service.CurrentProcessId!.Value;
                var status = await finished.Task.WaitAsync(TimeSpan.FromSeconds(8));
                Check("process gate completes/reaps " + mode, Gone(pid) && !service.IsSpeaking);
                Check("process status contains no private output " + mode, !status.Contains("PRIVATE_SPEECH_TEXT"));
                Check("owned scratch files removed " + mode, !Directory.Exists(File.ReadAllText(directoryReport)));
                if (mode == "invalid") Check("malformed metadata never becomes a highlight", cues.IsEmpty);
                if (mode == "ok") Check("normal metadata and audio timing cross bridge", cues.Count == 1 && service.LastAudioDuration == TimeSpan.FromMilliseconds(100));
                if (mode is "stderr" or "flood" or "hang") Check("process failure is not reported as completed speech " + mode, !status.Contains("færdig"));
            }
            Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_SPEECH_MODE", "slow");
            using var replacing = new WindowsTextToSpeechService(Factory);
            var started = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            replacing.PrecisePlaybackStarted += (sender, _) => started.TrySetResult(sender!);
            replacing.Speak("first", options); var oldPid = replacing.CurrentProcessId!.Value;
            var oldToken = await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_SPEECH_MODE", "ok");
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            replacing.SpeechStopped += (_, _) => done.TrySetResult();
            replacing.Speak("second", options); await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Check("replacement reaps old process and rejects queued old identity", Gone(oldPid) && !replacing.IsCurrent(oldToken));
            replacing.Stop(); Check("Stop invalidates even completed identity", !replacing.IsCurrent(oldToken) && !replacing.IsSpeaking);
        }
        finally { Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_SPEECH_MODE", null); Environment.SetEnvironmentVariable("WORDSUGGESTOR_TEST_SPEECH_DIRECTORY", null); }

        var measurements = new List<object>();
        foreach (var group in WindowsVoiceCatalogService.GetInstalledVoices().GroupBy(v => v.Source))
        {
            if (group.Key == "OneCore" && !WindowsTextToSpeechService.GetOneCoreRuntimeStatus().IsAvailable) continue;
            var voice = group.FirstOrDefault(v => v.LanguageCode == "da-DK") ?? group.First();
            var durations = new List<double>();
            foreach (var delta in new[] { -.5, 0, .5 })
            {
                using var service = new WindowsTextToSpeechService();
                var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                var cues = new ConcurrentQueue<TextToSpeechBoundaryCue>();
                service.SpeechStopped += (_, status) => done.TrySetResult(status); service.BoundaryCueReceived += (_, cue) => cues.Enqueue(cue);
                const string text = "Hej verden. Dette er en prøve.";
                var invocation = service.Speak(text, new(voice.LanguageCode, voice.Id, voice.DisplayName, voice.Source, "word", false, delta, null));
                var status = await done.Task.WaitAsync(TimeSpan.FromSeconds(30));
                Check("native playback completes " + group.Key + " " + SpeechSpeedPolicy.Percentage(delta), status.Contains("færdig") && service.LastAudioDuration.HasValue);
                Check("native word cues map to original text " + group.Key, cues.Count > 0 && cues.All(c => WindowsTextToSpeechService.ValidRange(text, c.Start, c.Length)));
                durations.Add(service.LastAudioDuration!.Value.TotalMilliseconds);
                measurements.Add(new { backend = invocation.Backend, percent = SpeechSpeedPolicy.Percentage(delta), audio_ms = durations.Last(), cue_count = cues.Count });
            }
            Check("native tempo changes in requested direction " + group.Key, durations[0] > durations[1] * 1.3 && durations[2] < durations[1] * .9);
            foreach (var mode in new[] { "sentence", "none", "unicode" })
            {
                using var service = new WindowsTextToSpeechService();
                var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                var cues = new ConcurrentQueue<TextToSpeechBoundaryCue>();
                service.SpeechStopped += (_, status) => done.TrySetResult(status); service.BoundaryCueReceived += (_, cue) => cues.Enqueue(cue);
                var text = mode == "unicode" ? "😀 æble a\u030a & <ord>." : "Hej verden. Næste sætning.";
                service.Speak(text, new(voice.LanguageCode, voice.Id, voice.DisplayName, voice.Source, mode == "unicode" ? "word" : mode, false, 0, null));
                Check("native highlight mode completes " + group.Key + " " + mode, (await done.Task.WaitAsync(TimeSpan.FromSeconds(30))).Contains("færdig"));
                Check("native highlight mode metadata " + group.Key + " " + mode, mode == "unicode"
                    ? new[] { "æble", "a\u030a", "ord" }.All(word => cues.Any(c => c.Start == text.IndexOf(word, StringComparison.Ordinal) && c.Length == word.Length))
                    : mode == "none" ? cues.IsEmpty : cues.Any(c => text.Substring(c.Start, c.Length).Trim() == "Hej verden."));
            }
        }
        var english = WindowsVoiceCatalogService.GetVoiceOptionsForLanguage("en-US");
        Check("voice list prefers exact locale before source quality", !english.Any(v => v.LanguageCode == "en-US") || english[0].LanguageCode == "en-US");
        Check("at least one real Windows speech backend qualified", measurements.Count >= 3);
        File.WriteAllText(Path.Combine(root, "playback-measurements.json"), JsonSerializer.Serialize(measurements, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS SETTINGS SPEECH {passed} checks; real native PCM timing/playback; audible quality not human-verified.");
    }
    private static bool Gone(int pid) { try { using var process = Process.GetProcessById(pid); return process.HasExited; } catch (ArgumentException) { return true; } }
}
