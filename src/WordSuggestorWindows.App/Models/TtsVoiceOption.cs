namespace WordSuggestorWindows.App.Models;

public sealed record TtsVoiceOption(
    string Id,
    string DisplayName,
    string LanguageCode,
    string Source,
    bool IsFallback = false)
{
    public string DisplayLabel => IsFallback
        ? $"{DisplayName} ({LanguageCode}, {Source}, fallback)"
        : $"{DisplayName} ({LanguageCode}, {Source})";
}
