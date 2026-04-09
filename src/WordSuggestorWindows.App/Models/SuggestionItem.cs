namespace WordSuggestorWindows.App.Models;

public sealed record SuggestionItem(
    string Term,
    double Score,
    string Kind
);
