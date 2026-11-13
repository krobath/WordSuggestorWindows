using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CoreUnknownWordHandoff([property: JsonRequired] int SchemaVersion,[property: JsonRequired] string Status,string? Body)
{ public override string ToString()=>$"UnknownWordHandoff[{SchemaVersion},{Status}]"; }
public interface ICoreUnknownWordHandoffAdapter
{
    Task<CoreQueryResult<CoreUnknownWordHandoff>> PrepareUnknownWordHandoffAsync(CoreRequestContext scope,int consentVersion,string clientVersion,CancellationToken token=default);
}
public static partial class UnknownWordHandoffPacket
{
    public const int MaximumBytes=1_048_576;
    public static (JsonElement Body,string Key) Parse(string body)
    {
        try { return ParseCore(body); }
        catch(Exception ex)when(ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException){throw Invalid();}
    }
    private static (JsonElement Body,string Key) ParseCore(string body)
    {
        if(Encoding.UTF8.GetByteCount(body)>MaximumBytes)throw new InvalidOperationException("Sharing packet is unavailable.");
        using var doc=JsonDocument.Parse(body);var root=doc.RootElement;
        Closed(root,["schema_version","generated_at_epoch","language","app_bundle","client_version","client_schema_version","retry_policy_version","idempotency_key","handoff"]);
        if(root.GetProperty("schema_version").GetString()!="unknown_word_crowd_handoff_ingest_v1"||root.GetProperty("language").GetString()!="da-DK"||root.GetProperty("app_bundle").GetString()!="WordSuggestor.Windows.Beta"||
           root.GetProperty("client_schema_version").GetString()!="wsx-sq-012-client-v1"||root.GetProperty("retry_policy_version").GetString()!="wsx-sq-012-retry-v1")throw Invalid();
        var key=root.GetProperty("idempotency_key").GetString();if(key is null||!KeyPattern().IsMatch(key))throw Invalid();
        var handoff=root.GetProperty("handoff");Closed(handoff,["schema_version","generated_at_epoch","language_code","learning_state_schema_version","entries","resolved","diagnostics"]);
        if(handoff.GetProperty("schema_version").GetString()!="unknown_word_crowd_handoff_v1"||handoff.GetProperty("language_code").GetString()!="da-DK"||
           handoff.GetProperty("entries").ValueKind!=JsonValueKind.Array||handoff.GetProperty("entries").GetArrayLength() is <1 or >8192||handoff.GetProperty("resolved").ValueKind!=JsonValueKind.Array)throw Invalid();
        foreach(var entry in handoff.GetProperty("entries").EnumerateArray())
        {
            Subset(entry,["language_code","term_norm","display","gram","lemma_norm","freq","source","source_record_id","phonetic_key","first_learned_epoch","last_refreshed_epoch","lookup_count"]);
            if(entry.GetProperty("language_code").GetString()!="da-DK"||entry.GetProperty("term_norm").GetString() is not {Length:>0 and <=512}||entry.GetProperty("display").GetString() is not {Length:>0 and <=512})throw Invalid();
        }
        foreach(var record in handoff.GetProperty("resolved").EnumerateArray())Subset(record,["key","language_code","term_norm","resolved_at_epoch","attempt_count","source"]);
        var diagnostics=handoff.GetProperty("diagnostics");Closed(diagnostics,["pending_count","failed_count","resolved_count","dropped_by_capacity","dropped_by_short_token","dropped_by_cooldown"]);
        if(diagnostics.EnumerateObject().Any(p=>!p.Value.TryGetInt32(out var n)||n<0)||diagnostics.GetProperty("resolved_count").GetInt32()!=handoff.GetProperty("resolved").GetArrayLength())throw Invalid();
        return(root.Clone(),key);
    }
    private static void Subset(JsonElement value,string[] allowed)
    {
        if(value.ValueKind!=JsonValueKind.Object)throw Invalid();var names=value.EnumerateObject().Select(p=>p.Name).ToArray();
        if(names.Distinct(StringComparer.Ordinal).Count()!=names.Length||names.Except(allowed,StringComparer.Ordinal).Any())throw Invalid();
    }
    private static void Closed(JsonElement value,string[] expected)
    {
        if(value.ValueKind!=JsonValueKind.Object)throw Invalid();var names=value.EnumerateObject().Select(p=>p.Name).ToArray();
        if(names.Length!=expected.Length||names.Distinct(StringComparer.Ordinal).Count()!=names.Length||names.Except(expected,StringComparer.Ordinal).Any())throw Invalid();
    }
    private static InvalidOperationException Invalid()=>new("Sharing packet is unavailable.");
    [GeneratedRegex("^uwh_[a-f0-9]{64}$",RegexOptions.CultureInvariant)]private static partial Regex KeyPattern();
}
