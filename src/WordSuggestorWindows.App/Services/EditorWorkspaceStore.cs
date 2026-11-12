using System.IO;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class EditorWorkspaceStore
{
    private const int MaxBytes = 64 * 1024 * 1024;
    private readonly object _gate = new();
    private long _lastRevision;
    private bool _canWrite = true;
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public string StorePath { get; }
    public string? LoadWarning { get; private set; }

    public EditorWorkspaceStore(string? path = null) => StorePath = path ?? Path.Combine(WindowsDataPaths.Root, "editor", "workspace-v1.json");

    public EditorWorkspace Load(string initialText = "")
    {
        if (!File.Exists(StorePath)) return new EditorWorkspace(initialText);
        try
        {
            if (new FileInfo(StorePath).Length > MaxBytes) throw new InvalidOperationException("Workspace too large.");
            var snapshot = JsonSerializer.Deserialize<EditorWorkspaceSnapshot>(File.ReadAllText(StorePath), Options)
                ?? throw new InvalidOperationException("Workspace unavailable.");
            return EditorWorkspace.Restore(snapshot);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or ArgumentException)
        {
            _canWrite = false; // preserve unreadable/newer drafts; explicit text-file Save still works
            LoadWarning = "Tidligere faner kunne ikke gendannes. De er bevaret. Nye tekster kan stadig gemmes som tekstfiler.";
            return new EditorWorkspace(initialText);
        }
    }

    public void Save(EditorWorkspaceSnapshot snapshot, long revision)
    {
        lock (_gate)
        {
            if (!_canWrite) throw new InvalidOperationException("Existing editor workspace is preserved.");
            if (revision < _lastRevision) return;
            if (File.Exists(StorePath))
            {
                if (new FileInfo(StorePath).Length > MaxBytes) throw new InvalidOperationException("Existing editor workspace is preserved.");
                using var previous = JsonDocument.Parse(File.ReadAllText(StorePath));
                if (!previous.RootElement.TryGetProperty("schemaVersion", out var schema) || !schema.TryGetInt32(out var version) || version != 1)
                    throw new InvalidOperationException("Existing editor workspace is preserved.");
                _ = EditorWorkspace.Restore(previous.RootElement.Deserialize<EditorWorkspaceSnapshot>(Options)
                    ?? throw new InvalidOperationException("Existing editor workspace is preserved."));
            }
            var data = JsonSerializer.SerializeToUtf8Bytes(snapshot, Options);
            _ = EditorWorkspace.Restore(snapshot); // refuse to persist a session the next launch could not restore
            if (data.Length > MaxBytes) throw new IOException("Editor workspace exceeds safe storage limit.");
            Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
            var temp = StorePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(data); stream.Flush(true); }
                if (File.Exists(StorePath)) File.Replace(temp, StorePath, StorePath + ".bak");
                else File.Move(temp, StorePath);
                _lastRevision = revision;
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}
