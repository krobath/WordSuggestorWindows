using System.Diagnostics;
using System.IO;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WordSuggestorCoreCliSuggestionProvider : ISuggestionProvider
{
    private const string DefaultLanguage = "da-DK";
    private const int DefaultMaxSuggestions = 40;
    private readonly string _workspaceRoot;
    private readonly string _coreRepoPath;
    private readonly string _packPath;
    private readonly string? _prebuiltCliPath;

    public WordSuggestorCoreCliSuggestionProvider()
    {
        _workspaceRoot = ResolveWorkspaceRoot();
        _coreRepoPath = Path.Combine(_workspaceRoot, "WordSuggestorCore");
        _packPath = Path.Combine(_coreRepoPath, "Ressources", "da_lexicon.sqlite");
        _prebuiltCliPath = ResolvePrebuiltCliPath();
    }

    public string ProviderDescription =>
        _prebuiltCliPath is not null
            ? $"WordSuggestorCore CLI ({Path.GetFileName(_prebuiltCliPath)})"
            : "WordSuggestorCore CLI (swift run fallback)";

    public async Task<IReadOnlyList<SuggestionItem>> SuggestAsync(
        string textBeforeCaret,
        CancellationToken cancellationToken)
    {
        var normalizedInput = textBeforeCaret.TrimEnd();
        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return [];
        }

        EnsureCoreArtifactsExist();

        var tempInputPath = Path.Combine(Path.GetTempPath(), $"wsw_input_{Guid.NewGuid():N}.txt");

        try
        {
            await File.WriteAllTextAsync(tempInputPath, normalizedInput + Environment.NewLine, cancellationToken);

            var startInfo = BuildStartInfo(tempInputPath);
            using var process = new Process { StartInfo = startInfo };

            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"WordSuggestorCore CLI exited with code {process.ExitCode}.{Environment.NewLine}{stderr}".Trim());
            }

            var rows = JsonSerializer.Deserialize<List<CliRequestRow>>(stdout, JsonOptions.Default);
            var firstRow = rows?.FirstOrDefault();

            return firstRow?.Suggestions?
                .Select(static suggestion => new SuggestionItem(suggestion.Term, suggestion.Score, suggestion.Kind))
                .ToList()
                ?? [];
        }
        finally
        {
            TryDelete(tempInputPath);
        }
    }

    private ProcessStartInfo BuildStartInfo(string inputPath)
    {
        if (_prebuiltCliPath is not null)
        {
            return new ProcessStartInfo
            {
                FileName = _prebuiltCliPath,
                Arguments = BuildCliArguments(inputPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _coreRepoPath,
            };
        }

        return new ProcessStartInfo
        {
            FileName = "swift",
            Arguments = $"run --package-path \"{_coreRepoPath}\" WordSuggestorSuggestCLI {BuildCliArguments(inputPath)}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _coreRepoPath,
        };
    }

    private string BuildCliArguments(string inputPath)
    {
        return $"--lang {DefaultLanguage} --pack \"{_packPath}\" --inputs \"{inputPath}\" --k {DefaultMaxSuggestions}";
    }

    private void EnsureCoreArtifactsExist()
    {
        if (!Directory.Exists(_coreRepoPath))
        {
            throw new DirectoryNotFoundException($"Could not locate WordSuggestorCore repository at '{_coreRepoPath}'.");
        }

        if (!File.Exists(_packPath))
        {
            throw new FileNotFoundException($"Could not locate Danish pack at '{_packPath}'.", _packPath);
        }
    }

    private string ResolveWorkspaceRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "WordSuggestorCore")) &&
                Directory.Exists(Path.Combine(current.FullName, "WordSuggestorWindows")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the workspace root containing both WordSuggestorCore and WordSuggestorWindows.");
    }

    private string? ResolvePrebuiltCliPath()
    {
        var configured = Environment.GetEnvironmentVariable("WORDSUGGESTOR_SUGGEST_CLI_PATH");
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            return configured;
        }

        var candidates = new[]
        {
            Path.Combine(_coreRepoPath, ".build", "debug", "WordSuggestorSuggestCLI.exe"),
            Path.Combine(_coreRepoPath, ".build", "debug", "WordSuggestorSuggestCLI"),
            Path.Combine(_coreRepoPath, ".build", "x86_64-unknown-windows-msvc", "debug", "WordSuggestorSuggestCLI.exe"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private sealed class CliRequestRow
    {
        public required string Input { get; init; }

        public required List<CliSuggestionRow> Suggestions { get; init; }
    }

    private sealed class CliSuggestionRow
    {
        public required string Term { get; init; }

        public required double Score { get; init; }

        public required string Kind { get; init; }
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNameCaseInsensitive = true,
        };
    }
}
