using System.IO;

namespace WordSuggestorWindows.App.Models;

public sealed record LanguageOption(
    string LanguageCode,
    string ShortLabel,
    string DisplayName,
    string PackTag,
    string? LegacyPackFileName)
{
    public string? PackPath { get; init; }
    public string FlagPattern { get; init; } = "HorizontalTricolor";
    public string FlagPrimaryBrush { get; init; } = "#D9DDE6";
    public string FlagSecondaryBrush { get; init; } = "#FFFFFF";
    public string FlagAccentBrush { get; init; } = "#B9C0CC";

    public bool IsPackAvailable => !string.IsNullOrWhiteSpace(PackPath);

    public string ToolbarLabel => IsPackAvailable
        ? ShortLabel
        : $"{ShortLabel} !";

    public string DisplayLabel => IsPackAvailable
        ? $"{ShortLabel} - {DisplayName}"
        : $"{ShortLabel} - {DisplayName} (mangler pakke)";

    public string PackAvailabilityLabel => IsPackAvailable
        ? $"Pakke: {Path.GetFileName(PackPath)}"
        : "Sprogpakke mangler";

    public override string ToString() => DisplayLabel;
}
