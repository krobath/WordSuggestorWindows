namespace WordSuggestorWindows.App.Models;

public sealed record SuggestionItem(
    string Term,
    double Score,
    string Kind,
    string Type,
    string? PartOfSpeech,
    string? Grammar
)
{
    public string? InsertText { get; init; }
    public CoreCommitPlan? CommitPlan { get; init; }
    public int Lane { get; init; }
}
