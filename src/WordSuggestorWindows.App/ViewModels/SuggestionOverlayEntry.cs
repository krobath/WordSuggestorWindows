using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.ViewModels;

public sealed record SuggestionOverlayEntry(int VisibleIndex, string ShortcutLabel, SuggestionItem Suggestion);
