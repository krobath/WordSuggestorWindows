using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed partial class WordSuggestorCoreCliSuggestionProvider : ISuggestionProvider, ICoreSettingsAdapter, ICoreUnknownWordAdapter, ICoreCompletedWordAdapter, ICoreUnknownWordHandoffAdapter
{
    private readonly string _baseDirectory;
    private readonly string? _workspaceRoot;
    private readonly string _localDataRoot;
    private readonly string? _cliPath;
    private readonly TimeSpan _timeout;
    public WindowsDiagnostics? Diagnostics { get; set; }
    private readonly SemaphoreSlim _processGate = new(1, 1);
    private LanguageOption _selectedLanguage;
    private readonly Dictionary<string, (long Length, DateTime Modified, PackValidation Result)> _validated = new();

    public WordSuggestorCoreCliSuggestionProvider(string? baseDirectory = null, string? localDataRoot = null,
        string? cliPath = null, TimeSpan? timeout = null)
    {
        _baseDirectory = baseDirectory ?? AppContext.BaseDirectory;
        _localDataRoot = localDataRoot ?? WindowsDataPaths.Root;
        _workspaceRoot = FindWorkspace(_baseDirectory);
        _timeout = timeout ?? TimeSpan.FromSeconds(8);
        _cliPath = cliPath ?? ResolveCli();
        LanguageOptions = SupportedLanguages.Select(language => language with { PackPath = ResolvePack(language) }).ToArray();
        _selectedLanguage = LanguageOptions.First();
    }

    public string ProviderDescription => $"WordSuggestorCore JSON v1 ({SelectedLanguage.ShortLabel})";
    public string? HealthWarning { get; private set; }
    public IReadOnlyList<LanguageOption> LanguageOptions { get; }
    public LanguageOption SelectedLanguage => _selectedLanguage;
    public void SetLanguage(LanguageOption language) => _selectedLanguage = LanguageOptions.FirstOrDefault(l => l.LanguageCode == language.LanguageCode) ?? LanguageOptions.First();

    public async Task<IReadOnlyList<SuggestionItem>> SuggestAsync(string textBeforeCaret, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(textBeforeCaret)) return [];
        var response = await ExecuteAsync("suggest", textBeforeCaret, cancellationToken);
        return response.Suggestions.Take(40).Select(row => new SuggestionItem(row.Term, row.Score, row.Kind, row.Type, row.Pos, row.Gram)).ToArray();
    }

    public async Task AddUserWordAsync(string word, CancellationToken cancellationToken = default) =>
        _ = await ExecuteAsync("addUserWord", word, cancellationToken);

    private async Task<BridgeResponse> ExecuteAsync(string operation, string text, CancellationToken cancellationToken)
    {
        var language = RequireLanguagePack();
        var requestId = Guid.NewGuid().ToString("N");
        var userWordsPath = Path.Combine(_localDataRoot, "UserWords", "local_lexicon_overlay_v2.json");
        Directory.CreateDirectory(Path.GetDirectoryName(userWordsPath)!);
        var request = new { version = 1, requestId, operation, language = language.LanguageCode,
            packPath = language.PackPath, textBeforeCaret = text.Length > 16000 ? text[^16000..] : text,
            maxSuggestions = 40, userWordsPath };
        var output = await ExchangeAsync(request, "--stdio-json-v1", cancellationToken, WindowsDiagnostics.Operation(operation));
        var response = Decode<BridgeResponse>(output);
        if (response.Version != 1 || response.RequestId != requestId)
            throw new InvalidOperationException("Core-protokollen er inkompatibel.");
        if (response.ErrorCode is not null) throw new InvalidOperationException("Core afviste forespørgslen.");
        HealthWarning = response.WarningCode == "user_words_unavailable" ? "Brugerord kunne ikke indlæses. Sprogpakken bruges fortsat, og brugerdata er bevaret." : null;
        if (response.Suggestions is null || response.Suggestions.Any(r => string.IsNullOrWhiteSpace(r.Term) || r.Term.Length > 512 || !double.IsFinite(r.Score)))
            throw new InvalidOperationException("Core returnerede ugyldige forslag.");
        return response;
    }

    private LanguageOption RequireLanguagePack()
    {
        var language = SelectedLanguage;
        if (language.PackPath is null || !ValidateCached(language.PackPath, language).Compatible)
        { Diagnostics?.Record(DiagnosticCategory.Core, DiagnosticEvent.CorePackUnavailable); throw new InvalidOperationException("Sprogpakken mangler eller er inkompatibel."); }
        if (_cliPath is null || !File.Exists(_cliPath))
        { Diagnostics?.Record(DiagnosticCategory.Core, DiagnosticEvent.CoreUnavailable); throw new FileNotFoundException("Core er ikke installeret."); }
        return language;
    }

    private static T Decode<T>(string output)
    {
        try { return JsonSerializer.Deserialize<T>(output, JsonOptions) ?? throw new InvalidOperationException("Ugyldigt Core-svar."); }
        catch (JsonException) { throw new InvalidOperationException("Ugyldigt Core-svar."); }
    }

    private async Task<string> ExchangeAsync(object request, string transport, CancellationToken cancellationToken, DiagnosticOperation operation = DiagnosticOperation.Legacy, TimeSpan? timeoutOverride = null)
    {
        var cliPath = _cliPath ?? throw new FileNotFoundException("Core er ikke installeret.");
        using var measurement = Diagnostics?.Measure(DiagnosticMetric.CoreRoundtrip);
        await _processGate.WaitAsync(cancellationToken);
        var outcome = DiagnosticEvent.CoreFailed;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeoutOverride ?? _timeout);
            var token = deadline.Token;
            var info = new ProcessStartInfo(cliPath)
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardInputEncoding = new UTF8Encoding(false), StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8, WorkingDirectory = Path.GetDirectoryName(cliPath)!
            };
            info.ArgumentList.Add(transport);
            // Pack/runtime logs must not contain user content, even on process failure.
            info.Environment["WORDSUGGESTOR_CORE_DEBUG"] = "0";
            using var process = new Process { StartInfo = info };
            process.Start();
            var stdout = ReadBoundedAsync(process.StandardOutput, 2 * 1024 * 1024, token);
            var stderr = ReadBoundedAsync(process.StandardError, 256 * 1024, token);
            try
            {
                await process.StandardInput.WriteAsync(JsonSerializer.Serialize(request).AsMemory(), token);
                process.StandardInput.Close();
                await process.WaitForExitAsync(token);
                var output = await stdout;
                _ = await stderr; // never forward raw process diagnostics into UI/logs
                if (process.ExitCode != 0) throw new InvalidOperationException($"Core exit code {process.ExitCode}.");
                outcome = DiagnosticEvent.CoreCompleted;
                return output;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            { outcome = DiagnosticEvent.CoreTimedOut; throw new TimeoutException("Core svarede ikke inden tidsgrænsen."); }
            catch (OperationCanceledException) { outcome = DiagnosticEvent.CoreCancelled; throw; }
            finally
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                    try { await process.WaitForExitAsync(cleanup.Token); } catch (OperationCanceledException) { }
                }
                try { await Task.WhenAll(stdout, stderr); } catch (Exception) { /* observed, no content logging */ }
            }
        }
        finally { _processGate.Release(); Diagnostics?.Record(DiagnosticCategory.Core, outcome, operation); }
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader, int maximum, CancellationToken token)
    {
        var result = new StringBuilder();
        var buffer = new char[4096];
        int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), token)) > 0)
        {
            if (result.Length + count > maximum) throw new InvalidOperationException("Core response exceeded limit.");
            result.Append(buffer, 0, count);
        }
        return result.ToString();
    }

    private PackValidation ValidateCached(string path, LanguageOption language)
    {
        if (!File.Exists(path)) return new(false, "missing");
        var file = new FileInfo(path);
        lock (_validated)
        {
            if (_validated.TryGetValue(path, out var cached) && cached.Length == file.Length && cached.Modified == file.LastWriteTimeUtc) return cached.Result;
            var result = LanguagePackValidator.Validate(path, language.PackTag,
                Path.GetFileName(path) == language.LegacyPackFileName);
            _validated[path] = (file.Length, file.LastWriteTimeUtc, result);
            return result;
        }
    }

    private string? ResolvePack(LanguageOption language)
    {
        var roots = new List<string> { Path.Combine(_localDataRoot, "Packs"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WordSuggestor", "Packs"),
            Path.Combine(_baseDirectory, "Packs") };
        if (_workspaceRoot is not null)
        {
            roots.Add(Path.Combine(_workspaceRoot, "WordSuggestorWindows", "Packs"));
            roots.Add(Path.Combine(_workspaceRoot, "WordSuggestorCore", "Ressources"));
        }
        foreach (var root in roots.Where(Directory.Exists))
        {
            var files = Directory.EnumerateFiles(root, $"{language.PackTag}_pack_v*.sqlite").ToList();
            files.Add(Path.Combine(root, $"{language.PackTag}_pack.sqlite"));
            if (language.LegacyPackFileName is not null) files.Add(Path.Combine(root, language.LegacyPackFileName));
            var valid = files.Where(File.Exists).Select(path => (Path: path, Result: ValidateCached(path, language)))
                .Where(pack => pack.Result.Compatible)
                .OrderByDescending(pack => Version.TryParse(pack.Result.Version, out var version) ? version : new Version(0, 0))
                .ThenBy(pack => pack.Path, StringComparer.Ordinal).FirstOrDefault();
            if (valid.Path is not null) return valid.Path;
        }
        return null;
    }

    private string? ResolveCli()
    {
        var bundled = Path.Combine(_baseDirectory, "Core", "WordSuggestorSuggestCLI.exe");
        if (WindowsUpdateService.ReadInstalledRelease() is not null || DirectInstallation.Current is not null) return File.Exists(bundled) ? bundled : null;
        var configured = Environment.GetEnvironmentVariable("WORDSUGGESTOR_SUGGEST_CLI_PATH");
        if (!string.IsNullOrWhiteSpace(configured)) return Path.GetFullPath(configured);
        if (File.Exists(bundled)) return bundled;
        if (_workspaceRoot is null) return null;
        return new[] { Path.Combine(_workspaceRoot, "WordSuggestorCore", ".build", "debug", "WordSuggestorSuggestCLI.exe"),
            Path.Combine(_workspaceRoot, "WordSuggestorCore", ".build", "x86_64-unknown-windows-msvc", "debug", "WordSuggestorSuggestCLI.exe") }.FirstOrDefault(File.Exists);
    }

    private static string? FindWorkspace(string directory)
    {
        for (var current = new DirectoryInfo(directory); current is not null; current = current.Parent)
            if (Directory.Exists(Path.Combine(current.FullName, "WordSuggestorCore")) && Directory.Exists(Path.Combine(current.FullName, "WordSuggestorWindows"))) return current.FullName;
        return null;
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


    private sealed record BridgeResponse(int Version, string RequestId, List<BridgeSuggestion> Suggestions, string? ErrorCode, string? WarningCode = null);
    private sealed record BridgeSuggestion(string Term, double Score, string Kind, string Type = "word", string? Pos = null, string? Gram = null);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
