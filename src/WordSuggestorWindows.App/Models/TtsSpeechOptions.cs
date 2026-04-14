namespace WordSuggestorWindows.App.Models;

public sealed record TtsSpeechOptions(
    string LanguageCode,
    string? VoiceId,
    string? VoiceDisplayName,
    string? VoiceSource,
    string ReadingHighlightMode,
    bool UseSystemSpeechSettings,
    double ReadingSpeedDelta,
    string? FallbackReason);
