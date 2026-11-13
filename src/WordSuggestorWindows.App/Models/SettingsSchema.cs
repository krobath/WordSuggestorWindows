namespace WordSuggestorWindows.App.Models;

// Device-local preferences. Credentials, document text, packs and learning
// consent are not inferred or created by this migration.
public static class SettingsSchema
{
    public const int CurrentVersion = 8;

    public static AppSettingsSnapshot Upgrade(AppSettingsSnapshot source)
    {
        if (source.SchemaVersion is < 1 or > CurrentVersion)
            throw new InvalidOperationException("Unsupported settings schema.");
        if (source.AdditionalProperties is null || source.SystemVoiceIdOverrideByLanguage is null ||
            source.SelectedLanguageCode is null || source.Suggestions is null || !double.IsFinite(source.ReadingSpeedDelta))
            throw new InvalidOperationException("Invalid settings values.");
        source.Suggestions.Validate();
        var result = source.Clone();
        if (source.SchemaVersion < 7 || source.UnknownWordLookupConsentVersion != 1)
        { result.IsUnknownWordLookupEnabled = false; result.UnknownWordLookupConsentVersion = 0; }
        if (!result.IsUnknownWordLookupEnabled) result.UnknownWordLookupConsentVersion = 0;
        if(source.SchemaVersion<8||result.UnknownWordAutomaticConsentVersion!=1||!result.IsAutomaticUnknownWordLookupEnabled||!result.IsUnknownWordLookupEnabled)
        {result.IsAutomaticUnknownWordLookupEnabled=false;result.UnknownWordAutomaticConsentVersion=0;}
        if (source.SchemaVersion == 1)
        {
            // Old Windows profiles had no interface preference or permission.
            // Retain their explicit placement; never derive UI language from it.
            result.InterfaceLocalePreference = "system_default";
            result.FollowCaretOptionEnabled = true;
        }
        result.SchemaVersion = CurrentVersion;
        if (source.SchemaVersion < 5 && result.PunctuationCommaStyle == "Standard")
            result.PunctuationCommaStyle = "grammatical_without_start_comma";
        if (result.PunctuationCommaStyle is not ("grammatical_without_start_comma" or "grammatical_with_start_comma"))
            throw new InvalidOperationException("Invalid comma style.");
        if (result.ContextualCommaMode is not ("boundary_local_contextual_v1" or "legacy_graph_only")) result.ContextualCommaMode = "legacy_graph_only";
        if (!result.SaveWordChoices) result.ShareWordChoices = false;
        if (result.InterfaceLocalePreference is not ("system_default" or "da-DK" or "en-GB"))
            throw new InvalidOperationException("Unsupported interface preference.");
        result.ReadingSpeedDelta = source.SchemaVersion < 3
            ? Services.SpeechSpeedPolicy.FromLegacy(source.ReadingSpeedDelta)
            : Services.SpeechSpeedPolicy.Normalize(source.ReadingSpeedDelta);
        result.ReadingStrategy = Services.TypingReadAloudPolicy.NormalizeStrategy(source.ReadingStrategy);
        result.Suggestions.Validate();
        if (!result.FollowCaretOptionEnabled) result.SuggestionPlacementMode = "static";
        return result;
    }
}
