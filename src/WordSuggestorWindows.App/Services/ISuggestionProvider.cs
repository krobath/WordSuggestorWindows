using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public interface ISuggestionProvider
{
    string ProviderDescription { get; }

    Task<IReadOnlyList<SuggestionItem>> SuggestAsync(
        string textBeforeCaret,
        CancellationToken cancellationToken);
}
