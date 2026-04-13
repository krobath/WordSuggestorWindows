namespace WordSuggestorWindows.App.Models;

public sealed record ErrorInsightEvent(
    DateTimeOffset Timestamp,
    string EventType,
    string LanguageCode,
    string? TypedText,
    string? AcceptedText,
    string? SuggestionKind,
    string? PartOfSpeech,
    int? Rank,
    string? Boundary);
