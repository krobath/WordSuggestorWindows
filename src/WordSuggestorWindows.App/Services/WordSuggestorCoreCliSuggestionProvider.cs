using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class WordSuggestorCoreCliSuggestionProvider : ISuggestionProvider
{
    private const int DefaultMaxSuggestions = 40;
    private readonly string _workspaceRoot;
    private readonly string _coreRepoPath;
    private readonly string? _prebuiltCliPath;
    private LanguageOption _selectedLanguage;

    public WordSuggestorCoreCliSuggestionProvider()
    {
        _workspaceRoot = ResolveWorkspaceRoot();
        _coreRepoPath = Path.Combine(_workspaceRoot, "WordSuggestorCore");
        _prebuiltCliPath = ResolvePrebuiltCliPath();
        LanguageOptions = BuildLanguageOptions();
        _selectedLanguage = LanguageOptions.FirstOrDefault(option => option.LanguageCode == "da-DK")
            ?? LanguageOptions.FirstOrDefault(option => option.IsPackAvailable)
            ?? LanguageOptions.First();
    }

    public string ProviderDescription =>
        _prebuiltCliPath is not null
            ? $"WordSuggestorCore CLI ({SelectedLanguage.ShortLabel}, {Path.GetFileName(_prebuiltCliPath)})"
            : $"WordSuggestorCore CLI ({SelectedLanguage.ShortLabel}, swift run fallback)";

    public IReadOnlyList<LanguageOption> LanguageOptions { get; }

    public LanguageOption SelectedLanguage => _selectedLanguage;

    public void SetLanguage(LanguageOption language)
    {
        var next = LanguageOptions.FirstOrDefault(option => option.LanguageCode == language.LanguageCode)
            ?? LanguageOptions.First();
        _selectedLanguage = next;
    }

    public async Task<IReadOnlyList<SuggestionItem>> SuggestAsync(
        string textBeforeCaret,
        CancellationToken cancellationToken)
    {
        var normalizedInput = textBeforeCaret.TrimEnd();
        if (string.IsNullOrWhiteSpace(normalizedInput))
        {
            return [];
        }

        var language = SelectedLanguage;
        if (!language.IsPackAvailable)
        {
            return [];
        }

        EnsureCoreArtifactsExist(language);

        var tempInputPath = Path.Combine(Path.GetTempPath(), $"wsw_input_{Guid.NewGuid():N}.txt");

        try
        {
            await File.WriteAllTextAsync(
                tempInputPath,
                normalizedInput + Environment.NewLine,
                Encoding.UTF8,
                cancellationToken);

            var startInfo = BuildStartInfo(tempInputPath, language);
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
                .Select(static suggestion => new SuggestionItem(
                    suggestion.Term,
                    suggestion.Score,
                    suggestion.Kind,
                    suggestion.Type,
                    suggestion.Pos,
                    suggestion.Gram))
                .ToList()
                ?? [];
        }
        finally
        {
            TryDelete(tempInputPath);
        }
    }

    private ProcessStartInfo BuildStartInfo(string inputPath, LanguageOption language)
    {
        if (_prebuiltCliPath is not null)
        {
            return new ProcessStartInfo
            {
                FileName = _prebuiltCliPath,
                Arguments = BuildCliArguments(inputPath, language),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _coreRepoPath,
            };
        }

        return new ProcessStartInfo
        {
            FileName = "swift",
            Arguments = $"run --package-path \"{_coreRepoPath}\" WordSuggestorSuggestCLI {BuildCliArguments(inputPath, language)}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _coreRepoPath,
        };
    }

    private string BuildCliArguments(string inputPath, LanguageOption language)
    {
        return $"--lang {language.LanguageCode} --pack \"{language.PackPath}\" --inputs \"{inputPath}\" --k {DefaultMaxSuggestions}";
    }

    private void EnsureCoreArtifactsExist(LanguageOption language)
    {
        if (!Directory.Exists(_coreRepoPath))
        {
            throw new DirectoryNotFoundException($"Could not locate WordSuggestorCore repository at '{_coreRepoPath}'.");
        }

        if (string.IsNullOrWhiteSpace(language.PackPath) || !File.Exists(language.PackPath))
        {
            throw new FileNotFoundException(
                $"Could not locate {language.DisplayName} pack for '{language.LanguageCode}'.",
                language.PackPath);
        }
    }

    private IReadOnlyList<LanguageOption> BuildLanguageOptions() =>
        SupportedLanguages
            .Select(template => template with { PackPath = ResolvePackPath(template) })
            .ToArray();

    private string? ResolvePackPath(LanguageOption language)
    {
        var searchRoots = new List<string>();
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrWhiteSpace(appData))
        {
            searchRoots.Add(Path.Combine(appData, "WordSuggestor", "Packs"));
        }

        searchRoots.Add(Path.Combine(_workspaceRoot, "WordSuggestorWindows", "Packs"));
        searchRoots.Add(Path.Combine(_coreRepoPath, "Ressources"));

        foreach (var root in searchRoots.Where(Directory.Exists))
        {
            var versioned = Directory
                .EnumerateFiles(root, $"{language.PackTag}_pack_v*.sqlite", SearchOption.TopDirectoryOnly)
                .OrderByDescending(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            if (versioned is not null)
            {
                return versioned;
            }

            var alias = Path.Combine(root, $"{language.PackTag}_pack.sqlite");
            if (File.Exists(alias))
            {
                return alias;
            }

            if (!string.IsNullOrWhiteSpace(language.LegacyPackFileName))
            {
                var legacy = Path.Combine(root, language.LegacyPackFileName);
                if (File.Exists(legacy))
                {
                    return legacy;
                }
            }
        }

        return null;
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

    private static readonly LanguageOption[] SupportedLanguages =
    [
        new("da-DK", "DA", "Dansk", "da_DK", "da_lexicon.sqlite")
        {
            FlagPattern = "NordicCross",
            FlagPrimaryBrush = "#B11E2E",
            FlagSecondaryBrush = "#FFFFFF"
        },
        new("en-US", "EN", "English", "en_US", "en_lexicon.sqlite")
        {
            FlagPattern = "UsFlag",
            FlagPrimaryBrush = "#B22234",
            FlagSecondaryBrush = "#FFFFFF",
            FlagAccentBrush = "#3C3B6E"
        },
        new("de-DE", "DE", "Deutsch", "de_DE", null)
        {
            FlagPattern = "HorizontalTricolor",
            FlagPrimaryBrush = "#1F1F1F",
            FlagSecondaryBrush = "#C62828",
            FlagAccentBrush = "#F2C94C"
        },
        new("fr-FR", "FR", "Francais", "fr_FR", null)
        {
            FlagPattern = "VerticalTricolor",
            FlagPrimaryBrush = "#244AA5",
            FlagSecondaryBrush = "#FFFFFF",
            FlagAccentBrush = "#D13D4A"
        },
        new("es-ES", "ES", "Espanol", "es_ES", null)
        {
            FlagPattern = "HorizontalTricolor",
            FlagPrimaryBrush = "#AA151B",
            FlagSecondaryBrush = "#F1BF00",
            FlagAccentBrush = "#AA151B"
        },
        new("it-IT", "IT", "Italiano", "it_IT", null)
        {
            FlagPattern = "VerticalTricolor",
            FlagPrimaryBrush = "#1E8F4D",
            FlagSecondaryBrush = "#FFFFFF",
            FlagAccentBrush = "#D13D4A"
        },
        new("sv-SE", "SV", "Svenska", "sv_SE", null)
        {
            FlagPattern = "NordicCross",
            FlagPrimaryBrush = "#1661A8",
            FlagSecondaryBrush = "#F0C84B"
        },
        new("nb-NO", "NB", "Norsk bokmal", "nb_NO", null)
        {
            FlagPattern = "NordicCrossDouble",
            FlagPrimaryBrush = "#C93B3B",
            FlagSecondaryBrush = "#FFFFFF",
            FlagAccentBrush = "#203A8E"
        },
        new("nn-NO", "NN", "Norsk nynorsk", "nn_NO", null)
        {
            FlagPattern = "NordicCrossDouble",
            FlagPrimaryBrush = "#C93B3B",
            FlagSecondaryBrush = "#FFFFFF",
            FlagAccentBrush = "#203A8E"
        }
    ];

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

        public string Type { get; init; } = "word";

        public string? Pos { get; init; }

        public string? Gram { get; init; }
    }

    private static class JsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            PropertyNameCaseInsensitive = true,
        };
    }
}
