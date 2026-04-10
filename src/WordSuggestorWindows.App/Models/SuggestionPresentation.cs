using System.Windows.Media;

namespace WordSuggestorWindows.App.Models;

public static class SuggestionPresentation
{
    public static Brush BackgroundBrushFor(string kind, bool isSelected)
    {
        if (isSelected)
        {
            return SelectedBackgroundBrush;
        }

        return Normalize(kind) switch
        {
            "phonetic" => PhoneticBackgroundBrush,
            "misspelling" => MisspellingBackgroundBrush,
            "synonym" => SynonymBackgroundBrush,
            _ => OrdinaryBackgroundBrush,
        };
    }

    public static Brush BorderBrushFor(bool isSelected) =>
        isSelected ? SelectedBorderBrush : DefaultBorderBrush;

    public static string MatchKindInlineLabel(string kind) =>
        Normalize(kind) switch
        {
            "prefix" => "almindelig",
            "phonetic" => "fonetisk",
            "misspelling" => "stavefejl",
            "synonym" => "synonym",
            "wildcard" => "wildcard",
            "fuzzy" => "fuzzy",
            "alias" => "alias",
            "confusionset" => "forveksling",
            _ => "almindelig",
        };

    public static string MatchKindHelpLabel(string kind) =>
        Normalize(kind) switch
        {
            "phonetic" => "Fonetisk forslag (lydbaseret).",
            "misspelling" => "Klassisk stavefejlskorrektion.",
            "synonym" => "Synonymforslag.",
            "wildcard" => "Wildcard-match.",
            "fuzzy" => "Fuzzy-match.",
            "alias" => "Alias-match.",
            "confusionset" => "Forslag fra et forvekslingssæt.",
            _ => "Almindeligt ordforslag.",
        };

    public static string PartOfSpeechLabel(string? partOfSpeech) =>
        Normalize(partOfSpeech) switch
        {
            "noun" => "Substantiv",
            "verb" => "Verbum",
            "adj" => "Adjektiv",
            "adv" => "Adverbium",
            "det" => "Artikel",
            "pron" => "Pronomen",
            "prep" => "Præposition",
            "conj" => "Konjunktion",
            "num" => "Talord",
            "other" => "Andet",
            _ => string.Empty,
        };

    public static string BuildMetadataSummary(SuggestionItem suggestion)
    {
        var parts = new List<string>();

        var posLabel = PartOfSpeechLabel(suggestion.PartOfSpeech);
        if (!string.IsNullOrWhiteSpace(posLabel))
        {
            parts.Add(posLabel);
        }

        if (!string.IsNullOrWhiteSpace(suggestion.Grammar))
        {
            parts.Add(suggestion.Grammar!);
        }

        return string.Join("  ", parts);
    }

    public static string BuildInfoSummary(SuggestionItem suggestion)
    {
        var parts = new List<string>
        {
            $"Type: {MatchKindInlineLabel(suggestion.Kind)}",
            $"Kategori: {MatchKindHelpLabel(suggestion.Kind)}"
        };

        if (!string.IsNullOrWhiteSpace(suggestion.Type))
        {
            parts.Add($"Kandidat: {suggestion.Type}");
        }

        var posLabel = PartOfSpeechLabel(suggestion.PartOfSpeech);
        if (!string.IsNullOrWhiteSpace(posLabel))
        {
            parts.Add($"Ordklasse: {posLabel}");
        }

        if (!string.IsNullOrWhiteSpace(suggestion.Grammar))
        {
            parts.Add($"Grammatik: {suggestion.Grammar}");
        }

        parts.Add($"Score: {suggestion.Score:F2}");
        return string.Join(Environment.NewLine, parts);
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("_", string.Empty, StringComparison.Ordinal)
                .Trim()
                .ToLowerInvariant();

    private static SolidColorBrush CreateBrush(byte alpha, byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromArgb(alpha, red, green, blue));
        brush.Freeze();
        return brush;
    }

    private static readonly SolidColorBrush OrdinaryBackgroundBrush = CreateBrush(255, 246, 250, 254);
    private static readonly SolidColorBrush PhoneticBackgroundBrush = CreateBrush(255, 248, 243, 203);
    private static readonly SolidColorBrush MisspellingBackgroundBrush = CreateBrush(255, 235, 225, 248);
    private static readonly SolidColorBrush SynonymBackgroundBrush = CreateBrush(255, 219, 235, 248);
    private static readonly SolidColorBrush SelectedBackgroundBrush = CreateBrush(255, 208, 226, 244);
    private static readonly SolidColorBrush DefaultBorderBrush = CreateBrush(255, 211, 222, 235);
    private static readonly SolidColorBrush SelectedBorderBrush = CreateBrush(255, 137, 172, 210);
}
