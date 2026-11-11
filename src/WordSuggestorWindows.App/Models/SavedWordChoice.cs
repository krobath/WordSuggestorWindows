namespace WordSuggestorWindows.App.Models;

public sealed record SavedWordChoice(Guid Id, DateTimeOffset CreatedAt, string Language, string Word,
    string ReadingId, string WordClass, string Grammar, string PackSha256)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string Interpretation => Services.SettingsText.WordClassLabel(WordClass) + " · " + Grammar;
    public bool BackendUploadEligible => false;
    public bool AdaptiveRankingEligible => false;
    public override string ToString() => $"SavedWordChoice id={Id}, language={Language}";
}
