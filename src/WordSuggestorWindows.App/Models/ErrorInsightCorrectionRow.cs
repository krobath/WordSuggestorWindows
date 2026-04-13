namespace WordSuggestorWindows.App.Models;

public sealed record ErrorInsightCorrectionRow(
    string TypedText,
    string AcceptedText,
    int Count);
