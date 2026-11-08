using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public interface ISuggestionProvider
{
    string ProviderDescription { get; }
    string? HealthWarning => null;

    IReadOnlyList<LanguageOption> LanguageOptions { get; }

    LanguageOption SelectedLanguage { get; }

    void SetLanguage(LanguageOption language);

    Task AddUserWordAsync(string word, CancellationToken cancellationToken = default) =>
        Task.FromException(new NotSupportedException("Brugerord er ikke tilgængelige."));

    Task<IReadOnlyList<SuggestionItem>> SuggestAsync(
        string textBeforeCaret,
        CancellationToken cancellationToken);
}
