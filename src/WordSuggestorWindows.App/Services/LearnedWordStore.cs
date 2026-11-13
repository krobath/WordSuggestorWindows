using System.IO;
using System.Text.Json;
using System.Security.Cryptography;

namespace WordSuggestorWindows.App.Services;

// Platform storage adapter. Core produces the complete snapshot and owns all
// lexical data/merging. Only this separate learned file is replaced, never user words.
public sealed class LearnedWordStore
{
    public static event EventHandler<string>? Changed;
    public const int MaximumBytes = 1_048_576;
    public string StorePath { get; }
    public LearnedWordStore(string? root = null) => StorePath = PathFor(root ?? WindowsDataPaths.Root);
    public static string PathFor(string root) => System.IO.Path.Combine(root, "UnknownWords", "learned-v1.json");
    public static void ValidateSnapshot(byte[] data)
    {
        if (data.Length > MaximumBytes) throw new InvalidOperationException("Learned word snapshot is unavailable.");
        using var json = JsonDocument.Parse(data);
        var root = json.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
            !root.TryGetProperty("schema_version", out var version) || version.GetString() != "local_lexicon_overlay_v2" ||
            !root.TryGetProperty("entries_by_language", out var tables) || tables.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Learned word snapshot is unavailable.");
    }
    public async Task<FileStream> AcquireAsync(CancellationToken token)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(StorePath)!);
        var deadline = Environment.TickCount64 + 2500;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            try { return new FileStream(StorePath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (Environment.TickCount64 < deadline) { await Task.Delay(50, token); }
        }
    }
    public void CheckExisting()
    {
        if (!File.Exists(StorePath)) return;
        if (new FileInfo(StorePath).Length > MaximumBytes) throw new InvalidOperationException("Existing learned words are preserved.");
        ValidateSnapshot(File.ReadAllBytes(StorePath));
    }
    private void CheckBackup()
    {
        if (!File.Exists(StorePath + ".bak")) return;
        if (new FileInfo(StorePath + ".bak").Length > MaximumBytes) throw new InvalidOperationException("Existing learned words are preserved.");
        ValidateSnapshot(File.ReadAllBytes(StorePath + ".bak"));
    }
    public string Fingerprint()
    {
        if (!File.Exists(StorePath)) return "missing";
        if (new FileInfo(StorePath).Length > MaximumBytes) throw new InvalidOperationException("Existing learned words are preserved.");
        return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(StorePath)));
    }
    public async Task CommitAsync(byte[] snapshot, Func<bool> current, CancellationToken token)
    {
        ValidateSnapshot(snapshot); CheckExisting(); CheckBackup();
        var temp = StorePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { await file.WriteAsync(snapshot, token); file.Flush(true); }
            token.ThrowIfCancellationRequested();
            // Production continuation is on the UI dispatcher: consent/close cannot
            // interleave between this final guard and synchronous atomic replacement.
            if (!current()) throw new OperationCanceledException();
            CheckExisting(); CheckBackup();
            if (File.Exists(StorePath)) File.Replace(temp, StorePath, StorePath + ".bak");
            else File.Move(temp, StorePath);
            Changed?.Invoke(this,StorePath);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    public async Task ClearAsync(CancellationToken token)
    {
        if (!File.Exists(StorePath) && !File.Exists(StorePath + ".bak")) return;
        using var lease = await AcquireAsync(token); CheckExisting();
        CheckBackup();
        token.ThrowIfCancellationRequested();
        if (File.Exists(StorePath + ".bak")) File.Delete(StorePath + ".bak");
        if (File.Exists(StorePath)) File.Delete(StorePath);
        Changed?.Invoke(this,StorePath);
    }
}
