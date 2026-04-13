using System.IO;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsErrorInsightsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly object _gate = new();
    private readonly string _storePath;

    public WindowsErrorInsightsStore()
    {
        _storePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WordSuggestor",
            "insights",
            "error-insights.jsonl");
    }

    public string StorePath => _storePath;

    public void RecordAcceptedSuggestion(
        string typedText,
        SuggestionItem suggestion,
        string languageCode,
        int? rank)
    {
        Append(new ErrorInsightEvent(
            DateTimeOffset.Now,
            "accepted-suggestion",
            languageCode,
            string.IsNullOrWhiteSpace(typedText) ? null : typedText,
            suggestion.Term,
            suggestion.Kind,
            suggestion.PartOfSpeech,
            rank,
            null));
    }

    public void RecordBackspace(string languageCode)
    {
        Append(new ErrorInsightEvent(
            DateTimeOffset.Now,
            "backspace",
            languageCode,
            null,
            null,
            null,
            null,
            null,
            null));
    }

    public void RecordSentenceBoundary(string languageCode, string boundary)
    {
        Append(new ErrorInsightEvent(
            DateTimeOffset.Now,
            "sentence-boundary",
            languageCode,
            null,
            null,
            null,
            null,
            null,
            boundary));
    }

    public ErrorInsightsSnapshot LoadSnapshot(int recentLimit = 30)
    {
        var events = ReadEvents();
        var accepted = events
            .Where(item => item.EventType == "accepted-suggestion")
            .ToArray();
        var lastSevenDaysCutoff = DateTimeOffset.Now.AddDays(-7);

        return new ErrorInsightsSnapshot(
            accepted.Length,
            events.Count(item => item.EventType == "backspace"),
            events.Count(item => item.EventType == "sentence-boundary"),
            events.Count(item => item.Timestamp >= lastSevenDaysCutoff),
            BuildBreakdown(accepted, item => item.SuggestionKind),
            BuildBreakdown(accepted, item => item.PartOfSpeech),
            BuildFrequentCorrections(accepted),
            events
                .OrderByDescending(item => item.Timestamp)
                .Take(recentLimit)
                .ToArray());
    }

    private void Append(ErrorInsightEvent insightEvent)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_storePath)!);
            File.AppendAllText(_storePath, JsonSerializer.Serialize(insightEvent, JsonOptions) + Environment.NewLine);
        }
    }

    private IReadOnlyList<ErrorInsightEvent> ReadEvents()
    {
        lock (_gate)
        {
            if (!File.Exists(_storePath))
            {
                return [];
            }

            return File
                .ReadLines(_storePath)
                .Select(TryDeserialize)
                .Where(item => item is not null)
                .Cast<ErrorInsightEvent>()
                .ToArray();
        }
    }

    private static ErrorInsightEvent? TryDeserialize(string line)
    {
        try
        {
            return JsonSerializer.Deserialize<ErrorInsightEvent>(line, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<ErrorInsightSummaryRow> BuildBreakdown(
        IReadOnlyList<ErrorInsightEvent> events,
        Func<ErrorInsightEvent, string?> selector) =>
        events
            .Select(selector)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .GroupBy(value => value!, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .Select(group => new ErrorInsightSummaryRow(group.Key, group.Count()))
            .ToArray();

    private static IReadOnlyList<ErrorInsightCorrectionRow> BuildFrequentCorrections(
        IReadOnlyList<ErrorInsightEvent> events) =>
        events
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.TypedText) &&
                !string.IsNullOrWhiteSpace(item.AcceptedText))
            .GroupBy(item => $"{item.TypedText!.ToUpperInvariant()}\u001F{item.AcceptedText!.ToUpperInvariant()}")
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.First().TypedText, StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .Select(group => new ErrorInsightCorrectionRow(
                group.First().TypedText!,
                group.First().AcceptedText!,
                group.Count()))
            .ToArray();
}
