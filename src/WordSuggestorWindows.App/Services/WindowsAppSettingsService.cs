using System.IO;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WindowsAppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly object _gate = new();
    private readonly string _settingsPath;

    public WindowsAppSettingsService()
    {
        _settingsPath = Path.Combine(
            WindowsDataPaths.Root,
            "settings",
            "settings-v1.json");
    }

    public string SettingsPath => _settingsPath;

    public AppSettingsSnapshot Load()
    {
        lock (_gate)
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettingsSnapshot();
            }

            try
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettingsSnapshot>(json, JsonOptions) ?? new AppSettingsSnapshot();
                if (settings.SchemaVersion != 1) return new AppSettingsSnapshot();
                settings.SystemVoiceIdOverrideByLanguage ??= [];
                settings.ReadingSpeedDelta = Math.Clamp(settings.ReadingSpeedDelta, -1, 1);
                return settings;
            }
            catch (JsonException)
            {
                return new AppSettingsSnapshot();
            }
            catch (IOException)
            {
                return new AppSettingsSnapshot();
            }
            catch (UnauthorizedAccessException)
            {
                return new AppSettingsSnapshot();
            }
        }
    }

    public void Save(AppSettingsSnapshot settings)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            if (File.Exists(_settingsPath))
            {
                try
                {
                    using var existing = JsonDocument.Parse(File.ReadAllText(_settingsPath));
                    if (existing.RootElement.TryGetProperty("schemaVersion", out var version) && version.GetInt32() > 1)
                        throw new InvalidOperationException("Settings created by a newer version are preserved.");
                }
                catch (JsonException) { /* keep the previous file as a recovery backup */ }
            }
            var tempPath = $"{_settingsPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));
                if (File.Exists(_settingsPath)) File.Replace(tempPath, _settingsPath, _settingsPath + ".bak");
                else File.Move(tempPath, _settingsPath);
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
        }
    }
}
