namespace WordSuggestorWindows.App.Models;

public sealed record ErrorInsightsSnapshot(
    int AcceptedSuggestionCount,
    int BackspaceCount,
    int SentenceBoundaryCount,
    int LastSevenDaysCount,
    IReadOnlyList<ErrorInsightSummaryRow> SuggestionKindBreakdown,
    IReadOnlyList<ErrorInsightSummaryRow> PartOfSpeechBreakdown,
    IReadOnlyList<ErrorInsightCorrectionRow> FrequentCorrections,
    IReadOnlyList<ErrorInsightEvent> RecentEvents);
