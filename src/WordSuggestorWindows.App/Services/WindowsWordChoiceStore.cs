using System.IO;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

// Local evaluation choices only. No document/context, upload queue or ranking.
public sealed class WindowsWordChoiceStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly object _gate = new();
    public string StorePath { get; }
    public WindowsWordChoiceStore(string? path = null) => StorePath = path ?? Path.Combine(WindowsDataPaths.Root, "learning", "word-choices-v1.json");
    public IReadOnlyList<SavedWordChoice> Load()
    {
        lock (_gate)
        {
            if (!File.Exists(StorePath)) return [];
            if (new FileInfo(StorePath).Length > 8 * 1024 * 1024) throw new InvalidOperationException("Learning data unavailable.");
            var data = JsonSerializer.Deserialize<ChoiceFile>(File.ReadAllText(StorePath), Json);
            if (data is null || data.SchemaVersion != 1 || data.Choices is null || data.Choices.Length > 5000 || data.Choices.Any(c => !Valid(c)) ||
                data.Choices.Select(c => c.Id).Distinct().Count() != data.Choices.Length) throw new InvalidOperationException("Learning data unavailable.");
            return data.Choices.OrderByDescending(c => c.CreatedAt).ToArray();
        }
    }
    public void Append(SavedWordChoice choice)
    {
        lock (_gate)
        {
            var current = Load();
            if (current.Count >= 5000) throw new InvalidOperationException("Learning store limit reached.");
            Write(current.Append(choice).ToArray());
        }
    }
    public void Delete(Guid id) { lock (_gate) Write(Load().Where(c => c.Id != id).ToArray()); }
    // Explicitly confirmed reset may replace corrupt/future data in this one store.
    public void Reset() { lock (_gate) Write([]); }
    private void Write(SavedWordChoice[] choices)
    {
        if (choices.Length > 5000 || choices.Any(c => !Valid(c)) || choices.Select(c => c.Id).Distinct().Count() != choices.Length) throw new InvalidOperationException("Invalid learning record.");
        var payload = JsonSerializer.Serialize(new ChoiceFile(1, choices), Json);
        if (System.Text.Encoding.UTF8.GetByteCount(payload) > 8 * 1024 * 1024) throw new InvalidOperationException("Learning store limit reached.");
        Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
        var temporary = StorePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, payload);
            if (File.Exists(StorePath)) File.Replace(temporary, StorePath, null);
            else File.Move(temporary, StorePath);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static bool Valid(SavedWordChoice? c) => c is not null && c.Id != Guid.Empty && !string.IsNullOrWhiteSpace(c.Language) && c.Language.Length <= 32 &&
        !string.IsNullOrWhiteSpace(c.Word) && c.Word.Length <= 128 && !string.IsNullOrWhiteSpace(c.ReadingId) && c.ReadingId.Length <= 4096 &&
        !string.IsNullOrWhiteSpace(c.WordClass) && c.WordClass.Length <= 64 && c.Grammar is not null && c.Grammar.Length <= 512 && c.PackSha256 is { Length: 64 } && c.PackSha256.All(Uri.IsHexDigit);
    private sealed record ChoiceFile(int SchemaVersion, SavedWordChoice[] Choices);
}
