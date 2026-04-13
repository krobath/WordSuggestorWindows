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
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WordSuggestor",
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
                return JsonSerializer.Deserialize<AppSettingsSnapshot>(json, JsonOptions) ?? new AppSettingsSnapshot();
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
            var tempPath = $"{_settingsPath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));

            if (File.Exists(_settingsPath))
            {
                File.Replace(tempPath, _settingsPath, null);
            }
            else
            {
                File.Move(tempPath, _settingsPath);
            }
        }
    }
}
