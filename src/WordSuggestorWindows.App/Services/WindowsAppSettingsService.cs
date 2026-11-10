using System.IO;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public enum SettingsLoadState { Missing, Current, Migrated, Unavailable }
public sealed record SettingsLoadResult(AppSettingsSnapshot Settings, SettingsLoadState State, int? OriginalSchemaVersion = null)
{
    public bool CanWrite => State != SettingsLoadState.Unavailable;
}

public sealed class WindowsAppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly object _gate = new();
    private readonly string _settingsPath;

    public WindowsAppSettingsService(string? settingsPath = null) =>
        _settingsPath = settingsPath ?? Path.Combine(WindowsDataPaths.Root, "settings", "settings-v1.json");

    // Keep the historical location. The schema in the file is versioned separately.
    public string SettingsPath => _settingsPath;
    public event EventHandler<AppSettingsSnapshot>? Changed;

    public AppSettingsSnapshot SaveChanges(AppSettingsSnapshot original, AppSettingsSnapshot edited)
    {
        AppSettingsSnapshot merged;
        lock (_gate)
        {
            var latest = ReadCurrent(); // strict: no recovery defaults during writes
            merged = SettingsPatch.Merge(original, edited, latest.Settings);
            Write(merged, latest.OriginalSchemaVersion);
        }
        Changed?.Invoke(this, merged.Clone());
        return merged;
    }

    public AppSettingsSnapshot Load() => LoadResult().Settings;

    public SettingsLoadResult LoadResult()
    {
        lock (_gate)
        {
            try { return ReadCurrent(); }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                // Continue offline with safe defaults; keep the source untouched
                // and refuse writes until the unavailable data is recovered.
                return new(new AppSettingsSnapshot
                {
                    IsGlobalCaptureEnabled = false, IsErrorTrackingEnabled = false,
                    FollowCaretOptionEnabled = false, SuggestionPlacementMode = "static"
                }, SettingsLoadState.Unavailable);
            }
        }
    }

    private SettingsLoadResult ReadCurrent()
    {
        if (!File.Exists(_settingsPath)) return new(new AppSettingsSnapshot(), SettingsLoadState.Missing);
        using var document = JsonDocument.Parse(File.ReadAllText(_settingsPath));
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidOperationException("Invalid settings document.");
        var version = 1;
        if (root.TryGetProperty("schemaVersion", out var field) &&
            (field.ValueKind != JsonValueKind.Number || !field.TryGetInt32(out version)))
            throw new InvalidOperationException("Invalid settings version.");
        if (version is < 1 or > SettingsSchema.CurrentVersion) throw new InvalidOperationException("Unsupported settings schema.");
        var settings = root.Deserialize<AppSettingsSnapshot>(JsonOptions) ?? throw new JsonException("Empty settings.");
        settings.SchemaVersion = version;
        return new(SettingsSchema.Upgrade(settings), version < SettingsSchema.CurrentVersion ? SettingsLoadState.Migrated : SettingsLoadState.Current,
            version < SettingsSchema.CurrentVersion ? version : null);
    }

    public void Save(AppSettingsSnapshot settings)
    {
        var normalized = SettingsSchema.Upgrade(settings);
        lock (_gate)
        {
            var latest = ReadCurrent();
            Write(normalized, latest.OriginalSchemaVersion);
        }
        Changed?.Invoke(this, normalized.Clone());
    }

    private void Write(AppSettingsSnapshot settings, int? migrateFrom)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
        // Once-only exact original-version bytes; subsequent .bak
        // snapshots are allowed to advance without destroying this migration copy.
        if (migrateFrom is { } version && !File.Exists(_settingsPath + $".v{version}.bak"))
            File.Copy(_settingsPath, _settingsPath + $".v{version}.bak", overwrite: false);
        var tempPath = $"{_settingsPath}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));
            if (File.Exists(_settingsPath)) File.Replace(tempPath, _settingsPath, _settingsPath + ".bak");
            else File.Move(tempPath, _settingsPath);
        }
        finally { if (File.Exists(tempPath)) File.Delete(tempPath); }
    }
}
