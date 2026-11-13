using System.Text.Json.Serialization;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CoreCompletedToken([property: JsonRequired] int SchemaVersion, int? Start, int? Length);
public interface ICoreCompletedWordAdapter
{
    Task<CoreQueryResult<CoreCompletedToken>> CompletedWordAsync(string prefix, int textOffset, CoreRequestContext scope, CancellationToken token = default);
}
