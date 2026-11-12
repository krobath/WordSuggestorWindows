using System.Text.Json;
using System.Text.Json.Serialization;

namespace WordSuggestorWindows.App.Models;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CoreUnknownWordResult([property: JsonRequired] int SchemaVersion, [property: JsonRequired] string Status,
    JsonElement? Proposal, string? Snapshot)
{
    public override string ToString() => $"UnknownWordResult schema={SchemaVersion} status={Status}";
}
