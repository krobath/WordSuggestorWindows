namespace WordSuggestorWindows.App.Models;

public sealed record SuggestionItem(
    string Term,
    double Score,
    string Kind,
    string Type,
    string? PartOfSpeech,
    string? Grammar
);
