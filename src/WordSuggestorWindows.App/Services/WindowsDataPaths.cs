using System.IO;

namespace WordSuggestorWindows.App.Services;

public static class WindowsDataPaths
{
    private static readonly Lazy<string?> LocalTestId = new(() =>
    {
        var name = WindowsUpdateService.ReadInstalledRelease()?.Name;
        const string prefix = "WordSuggestor.Windows.LocalTest.";
        return name is not null && name.StartsWith(prefix, StringComparison.Ordinal) &&
            System.Text.RegularExpressions.Regex.IsMatch(name[prefix.Length..], "^[A-Za-z0-9]{8,32}$") ? name[prefix.Length..] : null;
    });
    // Test isolation is explicit; ordinary upgrades keep the established location.
    public static string Root => Environment.GetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT") is { Length: > 0 } isolated
        ? Path.GetFullPath(isolated)
        : LocalTestId.Value is { } id
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WordSuggestor", "LocalTests", id)
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WordSuggestor");
}
