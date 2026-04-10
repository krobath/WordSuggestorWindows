namespace WordSuggestorWindows.App.ViewModels;

public sealed record AnalyzerLegendMetric(
    string Label,
    int Count,
    string MarkerBrush,
    bool UsesLineMarker = false);
