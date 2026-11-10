using System.Text.Json;
using System.Text.Json.Serialization;

namespace WordSuggestorWindows.App.Models;

public sealed record SuggestionPreferences
{
    public string Density { get; init; } = "fullDetails";
    public string Spacing { get; init; } = "smart";
    public int Responsiveness { get; init; } = 2;
    public string OrdinaryShortcut { get; init; } = "ctrl";
    public string PrimaryShortcut { get; init; } = "ctrlShift";
    public string SecondaryShortcut { get; init; } = "alt";
    public bool SpeakOnHover { get; init; } = true;
    public bool WordForms { get; init; }
    public bool LivePeriodAssist { get; init; } = true;
    public int IdleFadeSeconds { get; init; } = 7;
    public bool ContextBased { get; init; } = true;
    public bool ConfusionSet { get; init; } = true;
    public bool Phonetic { get; init; } = true;
    public bool Misspelling { get; init; } = true;
    public bool Synonym { get; init; } = true;
    public bool EditorWrap { get; init; } = true;
    public Dictionary<string, string> PrimaryDomains { get; init; } = [];
    public Dictionary<string, string> SecondaryDomains { get; init; } = [];
    [JsonExtensionData] public Dictionary<string, JsonElement> AdditionalProperties { get; init; } = [];
    public SuggestionPreferences Copy() => this with
    {
        PrimaryDomains = new(PrimaryDomains), SecondaryDomains = new(SecondaryDomains),
        AdditionalProperties = AdditionalProperties.ToDictionary(p => p.Key, p => p.Value.Clone())
    };
    public void Validate()
    {
        if (Density is not ("wordOnly" or "wordAndOrigin" or "fullDetails") || Spacing is not ("smart" or "always" or "never") ||
            Responsiveness is < 0 or > 4 || IdleFadeSeconds is < 0 or > 31 ||
            new[] { OrdinaryShortcut, PrimaryShortcut, SecondaryShortcut }.Any(s => s is not ("ctrl" or "ctrlShift" or "alt")) ||
            new[] { OrdinaryShortcut, PrimaryShortcut, SecondaryShortcut }.Distinct().Count() != 3 ||
            PrimaryDomains is null || SecondaryDomains is null || AdditionalProperties is null ||
            PrimaryDomains.Concat(SecondaryDomains).Any(p => p.Key.Length > 32 || string.IsNullOrWhiteSpace(p.Value) || p.Value.Length > 128) ||
            SecondaryDomains.Any(p => !PrimaryDomains.TryGetValue(p.Key, out var primary) || primary == p.Value))
            throw new InvalidOperationException("Invalid suggestion preferences or shortcut conflict.");
    }
}
