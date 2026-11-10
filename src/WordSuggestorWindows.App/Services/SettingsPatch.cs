using System.Text.Json;
using System.Text.Json.Nodes;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class SettingsConflictException : InvalidOperationException
{
    public SettingsConflictException() : base("Settings changed in another window.") { }
}

// A legacy form commits only fields the user changed. Unrelated/unknown fields
// and voice overrides for other languages belong to the latest stored document.
public static class SettingsPatch
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static AppSettingsSnapshot Merge(AppSettingsSnapshot original, AppSettingsSnapshot edited, AppSettingsSnapshot latest)
    {
        original = SettingsSchema.Upgrade(original);
        edited = SettingsSchema.Upgrade(edited);
        latest = SettingsSchema.Upgrade(latest);
        var before = JsonSerializer.SerializeToNode(original, Options)!.AsObject();
        var draft = JsonSerializer.SerializeToNode(edited, Options)!.AsObject();
        var merged = JsonSerializer.SerializeToNode(latest, Options)!.AsObject();
        MergeObject(before, draft, merged);
        return SettingsSchema.Upgrade(merged.Deserialize<AppSettingsSnapshot>(Options)!);
    }

    private static void MergeObject(JsonObject before, JsonObject draft, JsonObject latest)
    {
        foreach (var key in before.Select(p => p.Key).Union(draft.Select(p => p.Key)).ToArray())
        {
            before.TryGetPropertyValue(key, out var oldValue);
            draft.TryGetPropertyValue(key, out var newValue);
            if (JsonNode.DeepEquals(oldValue, newValue) && before.ContainsKey(key) == draft.ContainsKey(key)) continue;
            latest.TryGetPropertyValue(key, out var current);
            if (oldValue is JsonObject oldObject && newValue is JsonObject newObject && current is JsonObject currentObject)
            {
                MergeObject(oldObject, newObject, currentObject);
                continue;
            }
            var unchanged = JsonNode.DeepEquals(current, oldValue) && latest.ContainsKey(key) == before.ContainsKey(key);
            var alreadyApplied = JsonNode.DeepEquals(current, newValue) && latest.ContainsKey(key) == draft.ContainsKey(key);
            if (!unchanged && !alreadyApplied)
                throw new SettingsConflictException();
            if (draft.ContainsKey(key)) latest[key] = newValue?.DeepClone();
            else latest.Remove(key);
        }
    }
}
