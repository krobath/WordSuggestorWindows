using System.Text;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed partial class WordSuggestorCoreCliSuggestionProvider
{
    public Task<CoreQueryResult<CoreUnknownWordResult>> LookupUnknownWordAsync(string surface, CoreRequestContext scope, int consentVersion, CancellationToken token = default) =>
        UnknownWordQuery("lookupUnknownWord", surface, scope, null, consentVersion, token);
    public Task<CoreQueryResult<CoreUnknownWordResult>> PrepareLearnedWordAsync(string surface, JsonElement proposal, CoreRequestContext scope, int consentVersion, CancellationToken token = default) =>
        UnknownWordQuery("mergeLearnedWord", surface, scope, proposal, consentVersion, token);

    private async Task<CoreQueryResult<CoreUnknownWordResult>> UnknownWordQuery(string operation, string surface, CoreRequestContext scope, JsonElement? proposal, int consentVersion, CancellationToken token)
    {
        if (!Safe(surface, 128) || surface.Any(c => char.IsWhiteSpace(c) || char.IsControl(c))) throw new ArgumentException("Et enkelt ord kræves.");
        if (consentVersion != 1) throw new InvalidOperationException("Udtrykkeligt samtykke kræves.");
        var response = await QueryAsync(operation, surface, scope, null, null, null, token, proposal, consentVersion);
        var result = response.UnknownWord;
        if (result is not { SchemaVersion: 1 } || result.Status is not ("resolved" or "prepared" or "not_candidate" or "not_eligible" or "not_found" or "unavailable" or "unsupported_language" or "consent_required" or "invalid_proposal" or "store_full")) throw InvalidProtocol();
        if (result.Status == "resolved")
        {
            if (operation != "lookupUnknownWord" || result.Snapshot is not null || result.Proposal is not { ValueKind: JsonValueKind.Object } row) throw InvalidProtocol();
            var names = row.EnumerateObject().Select(p => p.Name).ToArray();
            if (names.Length != names.Distinct(StringComparer.Ordinal).Count() || names.Any(n => !new[] { "termNorm","display","gram","lemmaNorm","freq","source","sourceRecordID","phoneticKey" }.Contains(n))) throw InvalidProtocol();
            foreach (var required in new[] { "display","gram","source","sourceRecordID" })
                if (!row.TryGetProperty(required, out var value) || value.ValueKind != JsonValueKind.String || value.GetString()!.Length > 256) throw InvalidProtocol();
            foreach (var optional in new[] { "termNorm","lemmaNorm","phoneticKey" })
                if (row.TryGetProperty(optional, out var value) && (value.ValueKind != JsonValueKind.String || value.GetString()!.Length > 128)) throw InvalidProtocol();
            if (!row.TryGetProperty("freq", out var freq) || !freq.TryGetInt32(out var n) || n is < 1 or > 1_000_000) throw InvalidProtocol();
        }
        else if (result.Status == "prepared")
        {
            if (operation != "mergeLearnedWord" || result.Proposal is not null || result.Snapshot is null || Encoding.UTF8.GetByteCount(result.Snapshot) > LearnedWordStore.MaximumBytes) throw InvalidProtocol();
            LearnedWordStore.ValidateSnapshot(Encoding.UTF8.GetBytes(result.Snapshot));
        }
        else if (result.Proposal is not null || result.Snapshot is not null) throw InvalidProtocol();
        return Result(response, result);
    }
}
