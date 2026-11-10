using System.IO;
using System.Text.Json;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;

internal static class SettingsStateSmoke
{
    public static void Run(string root)
    {
        Directory.CreateDirectory(root);
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
        var future = original.Clone(); future.SchemaVersion = 2;
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
        Console.WriteLine($"PASS SETTINGS STATE {count} checks.");
    }
}
