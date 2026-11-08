using System.IO;

namespace WordSuggestorWindows.App.Services;

public static class WindowsDataPaths
{
    // Test isolation is explicit; ordinary upgrades keep the established location.
    public static string Root => Environment.GetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT") is { Length: > 0 } isolated
        ? Path.GetFullPath(isolated)
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WordSuggestor");
}
