using System.IO;
using System.Text.Json;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

internal static class SettingsStateSmoke
{
    public static void Run(string root)
    {
        Directory.CreateDirectory(root);
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", root);
        var count = 0;
        void Check(string title, bool ok) { if (!ok) throw new Exception(title); count++; Console.WriteLine("PASS SETTINGS STATE " + title); }
        void Reject<T>(string title, Action action) where T : Exception
        { var rejected = false; try { action(); } catch (T) { rejected = true; } Check(title, rejected); }
        var original = new AppSettingsSnapshot();
        original.SystemVoiceIdOverrideByLanguage["da-dk"] = "old-voice";
        original.AdditionalProperties["futureUiOption"] = JsonSerializer.SerializeToElement(new { enabled = true, value = "preserve" });
        var primary = original.Clone(); primary.IsErrorTrackingEnabled = false;
        var legacy = original.Clone(); legacy.IsGlobalCaptureEnabled = false;
        var first = SettingsPatch.Merge(original, primary, original);
        var merged = SettingsPatch.Merge(original, legacy, first);
        Check("two windows preserve independent changed fields", !merged.IsGlobalCaptureEnabled && !merged.IsErrorTrackingEnabled);
        Check("unknown current-schema data preserved", merged.AdditionalProperties["futureUiOption"].GetProperty("value").GetString() == "preserve");
        Check("draft does not mutate original", original.IsGlobalCaptureEnabled && original.IsErrorTrackingEnabled);
        var voiceA = original.Clone(); voiceA.SystemVoiceIdOverrideByLanguage["da-dk"] = "new-da";
        var voiceB = original.Clone(); voiceB.SystemVoiceIdOverrideByLanguage["en-us"] = "new-en";
        var voices = SettingsPatch.Merge(original, voiceA, SettingsPatch.Merge(original, voiceB, original));
        Check("voice overrides merge by language", voices.SystemVoiceIdOverrideByLanguage["da-dk"] == "new-da" && voices.SystemVoiceIdOverrideByLanguage["en-us"] == "new-en");
        var conflict = original.Clone(); conflict.ReadingSpeedDelta = .25;
        var newer = original.Clone(); newer.ReadingSpeedDelta = .5;
        Reject<SettingsConflictException>("overlapping different changes rejected", () => SettingsPatch.Merge(original, conflict, newer));
        Check("conflict leaves latest intact", newer.ReadingSpeedDelta == .5);
        Check("same concurrent value is idempotent", SettingsPatch.Merge(original, newer, newer).ReadingSpeedDelta == .5);
        var remove = original.Clone(); remove.SystemVoiceIdOverrideByLanguage.Remove("da-dk");
        // Removing a key changed elsewhere is itself a conflict.
        Reject<SettingsConflictException>("voice removal cannot erase newer voice", () => SettingsPatch.Merge(original, remove, voices));
        var compatible = original.Clone(); compatible.SystemVoiceIdOverrideByLanguage["en-us"] = "new-en";
        var removed = SettingsPatch.Merge(original, remove, compatible);
        Check("voice removal keeps independent language", !removed.SystemVoiceIdOverrideByLanguage.ContainsKey("da-dk") && removed.SystemVoiceIdOverrideByLanguage["en-us"] == "new-en");
        var future = original.Clone(); future.SchemaVersion = SettingsSchema.CurrentVersion + 1;
        Reject<InvalidOperationException>("future schema never patched", () => SettingsPatch.Merge(original, primary, future));
        var clone = original.Clone(); clone.AdditionalProperties["futureUiOption"] = JsonSerializer.SerializeToElement("changed");
        Check("unknown data cloned independently", original.AdditionalProperties["futureUiOption"].ValueKind == JsonValueKind.Object);
        var path = Path.Combine(root, "settings.json");
        var store = new WindowsAppSettingsService(path);
        store.Save(original);
        store.SaveChanges(original, primary);
        store.SaveChanges(original, legacy);
        var roundtrip = new WindowsAppSettingsService(path).Load();
        Check("file-backed legacy/primary changes survive restart", !roundtrip.IsErrorTrackingEnabled && !roundtrip.IsGlobalCaptureEnabled);
        Check("file-backed unknown data survives restart", roundtrip.AdditionalProperties.ContainsKey("futureUiOption"));
        Check("atomic settings update keeps backup", File.Exists(path + ".bak"));
        var baseline = store.Load(); var firstEdit = baseline.Clone(); firstEdit.ReadingSpeedDelta = .25;
        var secondEdit = baseline.Clone(); secondEdit.ReadingSpeedDelta = .5;
        store.SaveChanges(baseline, firstEdit); var stored = File.ReadAllText(path);
        Reject<SettingsConflictException>("stale dialog cannot overwrite latest file", () => store.SaveChanges(baseline, secondEdit));
        Check("conflict does not change stored bytes", File.ReadAllText(path) == stored);
        File.WriteAllText(path, "{\"schemaVersion\":99,\"future\":true}"); var futureBytes = File.ReadAllText(path);
        Reject<InvalidOperationException>("file-backed future settings preserved", () => store.SaveChanges(original, primary));
        Check("future bytes unchanged", File.ReadAllText(path) == futureBytes);
        File.WriteAllText(path, "{bad");
        Reject<JsonException>("corrupt current settings not replaced by stale form", () => store.SaveChanges(original, primary));
        Check("corrupt bytes retained", File.ReadAllText(path) == "{bad");
        Check("corrupt load clearly reports unavailable", store.LoadResult() is { CanWrite: false, State: SettingsLoadState.Unavailable });
        Reject<JsonException>("ordinary Save cannot overwrite corrupt settings", () => store.Save(original));

        var migrationPath = Path.Combine(root, "migration", "settings-v1.json");
        Directory.CreateDirectory(Path.GetDirectoryName(migrationPath)!);
        const string v1 = "{\"schemaVersion\":1,\"selectedLanguageCode\":\"en-US\",\"suggestionPlacementMode\":\"static\",\"isGlobalCaptureEnabled\":false,\"storeSentenceExamples\":false,\"systemVoiceIdOverrideByLanguage\":{\"en-us\":\"legacy-voice\"},\"sharedLearningOptIn\":false,\"futureChoice\":{\"enabled\":false}}";
        File.WriteAllText(migrationPath, v1, new System.Text.UTF8Encoding(true));
        var v1Bytes = File.ReadAllBytes(migrationPath);
        var migrationStore = new WindowsAppSettingsService(migrationPath);
        var migrated = migrationStore.LoadResult();
        Check("v1 upgraded in memory with explicit state", migrated is { State: SettingsLoadState.Migrated, CanWrite: true } && migrated.Settings.SchemaVersion == 2);
        Check("reading does not modify old settings", File.ReadAllText(migrationPath) == v1);
        Check("migration separates interface from English writing language", migrated.Settings.InterfaceLocalePreference == "system_default" && migrated.Settings.SelectedLanguageCode == "en-US");
        Check("migration retains explicit false choices and static placement", !migrated.Settings.IsGlobalCaptureEnabled && !migrated.Settings.StoreSentenceExamples && migrated.Settings.SuggestionPlacementMode == "static");
        Check("migration does not create consent", !migrated.Settings.AdditionalProperties["sharedLearningOptIn"].GetBoolean());
        var editedMigration = migrated.Settings.Clone(); editedMigration.InterfaceLocalePreference = "da-DK";
        var notifications = 0; migrationStore.Changed += (_, _) => notifications++;
        migrationStore.SaveChanges(migrated.Settings, editedMigration);
        Check("first change writes current schema with exact v1 backup", migrationStore.LoadResult().State == SettingsLoadState.Current && File.ReadAllText(migrationPath + ".v1.bak") == v1);
        Check("migration preserves raw v1 bytes including UTF-8 BOM", File.ReadAllBytes(migrationPath + ".v1.bak").SequenceEqual(v1Bytes));
        Check("successful atomic write emits exactly one notification", notifications == 1);
        Check("new interface choice leaves writing and voice unchanged", migrationStore.Load().SelectedLanguageCode == "en-US" && migrationStore.Load().SystemVoiceIdOverrideByLanguage["en-us"] == "legacy-voice");
        var next = migrationStore.Load(); var nextEdit = next.Clone(); nextEdit.IsErrorTrackingEnabled = false;
        migrationStore.SaveChanges(next, nextEdit);
        Check("later saves keep original migration backup", File.ReadAllText(migrationPath + ".v1.bak") == v1);
        Check("unknown object survives migration and later saves", !migrationStore.Load().AdditionalProperties["futureChoice"].GetProperty("enabled").GetBoolean());
        Check("missing file is a writable new profile", new WindowsAppSettingsService(Path.Combine(root, "absent.json")).LoadResult() is { State: SettingsLoadState.Missing, CanWrite: true });
        File.WriteAllText(path, "{\"schemaVersion\":99}");
        Check("unavailable profile disables external capture and content collection", store.Load() is { IsGlobalCaptureEnabled: false, IsErrorTrackingEnabled: false, StoreSentenceExamples: false, FollowCaretOptionEnabled: false });
        foreach (var invalid in new[] { "{\"schemaVersion\":3}", "{\"schemaVersion\":0}", "{\"schemaVersion\":\"2\"}", "{\"schemaVersion\":999999999999999999999}", "[]", "{\"schemaVersion\":2,\"interfaceLocalePreference\":\"unknown\"}", "{\"schemaVersion\":2,\"systemVoiceIdOverrideByLanguage\":null}" })
        {
            File.WriteAllText(path, invalid);
            Check("unsupported/invalid document stays unavailable", !store.LoadResult().CanWrite && File.ReadAllText(path) == invalid);
            Reject<Exception>("unsupported/invalid document never overwritten", () => store.Save(original));
        }
        Check("complete matching Danish and English catalogs", SettingsText.CatalogsMatch && SettingsText.Keys.Count >= 150);
        Check("explicit Danish ignores system English", SettingsText.ResolveLocale("da-DK", "en-US") == "da-DK");
        Check("explicit English ignores system Danish", SettingsText.ResolveLocale("en-GB", "da-DK") == "en-GB");
        Check("system Danish resolved separately", SettingsText.ResolveLocale("system_default", "da_DK") == "da-DK");
        Check("unsupported system language has English fallback", SettingsText.ResolveLocale("system_default", "de-DE") == "en-GB");

        var runtimeStore = new WindowsAppSettingsService(Path.Combine(root, "runtime", "settings.json"));
        runtimeStore.Save(new() { InterfaceLocalePreference = "da-DK" });
        var provider = new WordSuggestorCoreCliSuggestionProvider(localDataRoot: root);
        using var vm = new WordSuggestorWindows.App.ViewModels.MainWindowViewModel(provider, new WindowsErrorInsightsStore(), runtimeStore, "");
        var start = vm.LoadSettingsSnapshot(); var disable = start.Clone(); disable.FollowCaretOptionEnabled = false;
        Check("permission change saves successfully", vm.TryApplySettingsChanges(start, disable));
        Check("disabling permission normalizes runtime and file to static", !vm.FollowCaretOptionEnabled && vm.IsStaticPlacementMode && !vm.RequestsCaretGeometry && runtimeStore.Load().SuggestionPlacementMode == "static");
        vm.SetSuggestionPlacementMode(SuggestionPlacementMode.FollowCaret);
        Check("direct follow request cannot bypass permission", vm.IsStaticPlacementMode && !vm.RequestsCaretGeometry);
        var off = vm.LoadSettingsSnapshot(); var enable = off.Clone(); enable.FollowCaretOptionEnabled = true;
        vm.TryApplySettingsChanges(off, enable);
        Check("enabling permission does not request follow or geometry", vm.FollowCaretOptionEnabled && vm.IsStaticPlacementMode && !vm.RequestsCaretGeometry);
        vm.SetSuggestionPlacementMode(SuggestionPlacementMode.FollowCaret);
        Check("explicit permitted follow requests geometry", vm.RequestsCaretGeometry && runtimeStore.Load().SuggestionPlacementMode == "followCaret");
        var beforeUi = vm.LoadSettingsSnapshot(); var english = beforeUi.Clone(); english.InterfaceLocalePreference = "en-GB";
        vm.TryApplySettingsChanges(beforeUi, english);
        Check("interface changes at runtime without changing writing language", SettingsText.ResolvedLocale == "en-GB" && vm.SelectedLanguageOption.LanguageCode == beforeUi.SelectedLanguageCode);
        var missing = vm.LanguageOptions.First(l => !l.IsPackAvailable);
        vm.SelectedLanguageOption = missing;
        Check("toolbar cannot select an unavailable pack", vm.SelectedLanguageOption.LanguageCode == beforeUi.SelectedLanguageCode && runtimeStore.Load().SelectedLanguageCode == beforeUi.SelectedLanguageCode);
        var originalRuntime = vm.IsGlobalCaptureEnabled;
        File.WriteAllText(runtimeStore.SettingsPath, "{broken");
        vm.IsGlobalCaptureEnabled = !originalRuntime;
        Check("failed toolbar save leaves runtime unchanged", vm.IsGlobalCaptureEnabled == originalRuntime && File.ReadAllText(runtimeStore.SettingsPath) == "{broken");
        Check("unavailable file keeps last-applied preference presentation", vm.LoadSettingsSnapshot().IsGlobalCaptureEnabled == originalRuntime && vm.LoadSettingsSnapshot().InterfaceLocalePreference == "en-GB" && !vm.CanWriteSettings);
        Console.WriteLine($"PASS SETTINGS STATE {count} checks.");
    }
}
