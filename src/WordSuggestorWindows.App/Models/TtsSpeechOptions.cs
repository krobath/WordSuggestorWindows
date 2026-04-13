namespace WordSuggestorWindows.App.Models;

public sealed record TtsSpeechOptions(
    string LanguageCode,
    string? VoiceId,
    string? VoiceDisplayName,
    bool UseSystemSpeechSettings,
    double ReadingSpeedDelta,
    string? FallbackReason);
