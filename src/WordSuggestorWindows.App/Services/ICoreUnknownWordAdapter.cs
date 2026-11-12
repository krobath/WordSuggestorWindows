using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public interface ICoreUnknownWordAdapter
{
    Task<CoreQueryResult<CoreUnknownWordResult>> LookupUnknownWordAsync(string surface, CoreRequestContext scope, int consentVersion, CancellationToken token = default);
    Task<CoreQueryResult<CoreUnknownWordResult>> PrepareLearnedWordAsync(string surface, JsonElement proposal, CoreRequestContext scope, int consentVersion, CancellationToken token = default);
}
