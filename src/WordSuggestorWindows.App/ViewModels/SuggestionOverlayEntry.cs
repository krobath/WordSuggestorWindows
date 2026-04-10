using System.Windows.Media;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.ViewModels;

public sealed record SuggestionOverlayEntry(
    int VisibleIndex,
    string ShortcutLabel,
    SuggestionItem Suggestion,
    bool IsSelected)
{
    public string KindInlineSummary => $"({SuggestionPresentation.MatchKindInlineLabel(Suggestion.Kind)})";

    public string MetadataSummary => SuggestionPresentation.BuildMetadataSummary(Suggestion);

    public bool HasMetadataSummary => !string.IsNullOrWhiteSpace(MetadataSummary);

    public string MatchKindHelpText => SuggestionPresentation.MatchKindHelpLabel(Suggestion.Kind);

    public string InfoSummary => SuggestionPresentation.BuildInfoSummary(Suggestion);

    public Brush BackgroundBrush => SuggestionPresentation.BackgroundBrushFor(Suggestion.Kind, IsSelected);

    public Brush BorderBrush => SuggestionPresentation.BorderBrushFor(IsSelected);
}
