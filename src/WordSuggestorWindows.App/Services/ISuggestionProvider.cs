using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public interface ISuggestionProvider
{
    string ProviderDescription { get; }

    IReadOnlyList<LanguageOption> LanguageOptions { get; }

    LanguageOption SelectedLanguage { get; }

    void SetLanguage(LanguageOption language);

    Task<IReadOnlyList<SuggestionItem>> SuggestAsync(
        string textBeforeCaret,
        CancellationToken cancellationToken);
}
