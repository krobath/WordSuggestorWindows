namespace WordSuggestorWindows.App.Models;

public sealed record ExternalSuggestionCommitRequest(
    SuggestionItem Suggestion,
    string TypedToken,
    int ReplaceCharacterCount,
    IntPtr WindowHandle);
