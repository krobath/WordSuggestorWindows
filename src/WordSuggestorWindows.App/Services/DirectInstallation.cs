using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WordSuggestorWindows.App.Services;

public sealed record DirectInstallation(string ProductId, string PackageVersion, string DisplayVersion, string Publisher, bool UpdatesEnabled)
{
    private static readonly Lazy<DirectInstallation?> Installed = new(() => Read(AppContext.BaseDirectory));
    public static DirectInstallation? Current => Installed.Value;
    public bool IsPreview => ProductId.StartsWith("WordSuggestor.Windows.Preview.", StringComparison.Ordinal);
    public string? PreviewId => IsPreview ? ProductId["WordSuggestor.Windows.Preview.".Length..] : null;
    public static DirectInstallation? Read(string directory)
    {
        var path = Path.Combine(directory, "direct-install.json");
        try
        {
            if (!File.Exists(path) || new FileInfo(path).Length > 4096) return null;
            using var json = JsonDocument.Parse(File.ReadAllText(path));
            var r = json.RootElement;
            if (r.GetProperty("schema_version").GetInt32() != 1) return null;
            var id = r.GetProperty("product_id").GetString() ?? "";
            var version = r.GetProperty("package_version").GetString() ?? "";
            var display = r.GetProperty("display_version").GetString() ?? "";
            var publisher = r.GetProperty("publisher").GetString() ?? "";
            var updates = r.GetProperty("updates_enabled").GetBoolean();
            if ((id != "WordSuggestor.Windows.Beta.Direct" && !Regex.IsMatch(id, @"^WordSuggestor\.Windows\.Preview\.[a-f0-9]{12}$")) ||
                !Regex.IsMatch(version, @"^\d{1,5}\.\d{1,5}\.\d{1,5}\.\d{1,5}$") || !Version.TryParse(version, out var parsed) ||
                new[] { parsed.Major, parsed.Minor, parsed.Build, parsed.Revision }.Any(n => n > 65535) ||
                display.Length is 0 or > 128 || display.Any(char.IsControl) || publisher.Length is 0 or > 1024 || publisher.Any(char.IsControl) ||
                (id.StartsWith("WordSuggestor.Windows.Preview.", StringComparison.Ordinal) && updates)) return null;
            return new(id, version, display, publisher, updates);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or KeyNotFoundException or FormatException) { return null; }
    }
}
