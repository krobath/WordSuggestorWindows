namespace WordSuggestorWindows.App.Models;

// Device-local preferences. Credentials, document text, packs and learning
// consent are not inferred or created by this migration.
public static class SettingsSchema
{
    public const int CurrentVersion = 2;

    public static AppSettingsSnapshot Upgrade(AppSettingsSnapshot source)
    {
        if (source.SchemaVersion is < 1 or > CurrentVersion)
            throw new InvalidOperationException("Unsupported settings schema.");
        if (source.AdditionalProperties is null || source.SystemVoiceIdOverrideByLanguage is null ||
            source.SelectedLanguageCode is null || !double.IsFinite(source.ReadingSpeedDelta))
            throw new InvalidOperationException("Invalid settings values.");
        var result = source.Clone();
        if (source.SchemaVersion == 1)
        {
            // Old Windows profiles had no interface preference or permission.
            // Retain their explicit placement; never derive UI language from it.
            result.InterfaceLocalePreference = "system_default";
            result.FollowCaretOptionEnabled = true;
        }
        result.SchemaVersion = CurrentVersion;
        if (result.InterfaceLocalePreference is not ("system_default" or "da-DK" or "en-GB"))
            throw new InvalidOperationException("Unsupported interface preference.");
        result.ReadingSpeedDelta = Math.Clamp(result.ReadingSpeedDelta, -1, 1);
        if (!result.FollowCaretOptionEnabled) result.SuggestionPlacementMode = "static";
        return result;
    }
}
