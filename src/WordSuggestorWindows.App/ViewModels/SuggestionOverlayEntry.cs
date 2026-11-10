using System.Windows.Media;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.ViewModels;

public sealed record SuggestionOverlayEntry(
    int VisibleIndex,
    string ShortcutLabel,
    SuggestionItem Suggestion,
    bool IsSelected)
{
    public string Density { get; init; } = "fullDetails";
    public double RowHeight => Density switch { "wordOnly" => 34, "wordAndOrigin" => 40, _ => 48 };
    public string KindInlineSummary => Density == "wordOnly" ? "" : $"({SuggestionPresentation.MatchKindInlineLabel(Suggestion.Kind)})";

    public string MetadataSummary => SuggestionPresentation.BuildMetadataSummary(Suggestion);

    public bool HasMetadataSummary => Density == "fullDetails" && !string.IsNullOrWhiteSpace(MetadataSummary);

    public string MatchKindHelpText => SuggestionPresentation.MatchKindHelpLabel(Suggestion.Kind);

    public string InfoSummary => SuggestionPresentation.BuildInfoSummary(Suggestion);

    public Brush BackgroundBrush => SuggestionPresentation.BackgroundBrushFor(Suggestion.Kind, IsSelected);

    public Brush BorderBrush => SuggestionPresentation.BorderBrushFor(IsSelected);
}
