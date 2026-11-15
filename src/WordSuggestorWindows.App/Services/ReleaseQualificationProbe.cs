using System.IO;
using System.Text.Json;

namespace WordSuggestorWindows.App.Services;

// Explicit installed-release diagnostics. Synthetic fixtures only; no document,
// keystrokes, raw process errors or credentials are written to the result.
public static class ReleaseQualificationProbe
{
    private static InstalledRelease? QualificationIdentity() => WindowsUpdateService.ReadInstalledRelease() ??
        (DirectInstallation.Current is { IsPreview: true } preview ? new(preview.ProductId, preview.Publisher, Version.Parse(preview.PackageVersion)) : null);
    public static void ConfigureRoot(IReadOnlyList<string> args)
    {
        var index = args.ToList().IndexOf("--qualification-root");
        if (index < 0) return;
        var identity = QualificationIdentity() ?? throw new InvalidOperationException("Installed identity required.");
        if (identity.Name != WindowsUpdateService.PackageName && !identity.Name.StartsWith("WordSuggestor.Windows.LocalTest.", StringComparison.Ordinal) && !identity.Name.StartsWith("WordSuggestor.Windows.Preview.", StringComparison.Ordinal))
            throw new InvalidOperationException("Unknown qualification identity.");
        if (index + 1 >= args.Count) throw new ArgumentException("Qualification root required.");
        var root = Path.GetFullPath(args[index + 1]);
        var allowed = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WordSuggestor");
        if (identity.Name.StartsWith("WordSuggestor.Windows.LocalTest.", StringComparison.Ordinal))
            allowed = Path.Combine(allowed, "LocalTests", identity.Name["WordSuggestor.Windows.LocalTest.".Length..]);
        if (identity.Name.StartsWith("WordSuggestor.Windows.Preview.", StringComparison.Ordinal))
            allowed = Path.Combine(allowed, "LocalTests", identity.Name["WordSuggestor.Windows.Preview.".Length..]);
        if (!root.Equals(allowed, StringComparison.OrdinalIgnoreCase) && !root.StartsWith(allowed + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Qualification root is outside this identity's user-data area.");
        Environment.SetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT", root);
    }

    public static async Task<int?> TryRunAsync(IReadOnlyList<string> args)
    {
        var index = args.ToList().IndexOf("--qualification-probe");
        if (index < 0) return null;
        var resultIndex = args.ToList().IndexOf("--qualification-result");
        if (index + 1 >= args.Count || resultIndex < 0 || resultIndex + 1 >= args.Count || !args.Contains("--qualification-root")) return 2;
        var mode = args[index + 1];
        if (mode is not ("add" or "check")) return 2;
        var identity = QualificationIdentity();
        if (identity is null) return 2;
        var root = WindowsDataPaths.Root;
        var result = Path.GetFullPath(args[resultIndex + 1]);
        if (!IsReservedResultPath(root, result)) return 2;
        bool ok = false;
        string? error = null;
        try
        {
            var provider = new WordSuggestorCoreCliSuggestionProvider();
            if (mode == "add") await provider.AddUserWordAsync("betaupdateringssolsikke");
            ok = (await provider.SuggestAsync("betaupdateringssol", default)).Any(s => s.Term == "betaupdateringssolsikke") &&
                (await provider.SuggestAsync("skri", default)).Any(s => s.Term == "skrive");
        }
        catch (Exception ex) { error = ex.GetType().Name; }
        Directory.CreateDirectory(Path.GetDirectoryName(result)!);
        await using var stream = new FileStream(result, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, new
        {
            schema_version = 1, ok, package_name = identity.Name, package_version = identity.Version.ToString(), error_category = error
        });
        return ok ? 0 : 1;
    }

    internal static bool IsReservedResultPath(string root, string result)
    {
        var fullResult = Path.GetFullPath(result);
        var diagnostics = Path.Combine(Path.GetFullPath(root), "diagnostics");
        return string.Equals(Path.GetDirectoryName(fullResult), diagnostics, StringComparison.OrdinalIgnoreCase) &&
            System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileName(fullResult), "\\Aqualification-[a-f0-9]{32}\\.json\\z") &&
            !File.Exists(fullResult);
    }
}
