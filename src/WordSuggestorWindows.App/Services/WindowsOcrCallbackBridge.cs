using System.IO;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public static class WindowsOcrCallbackBridge
{
    public const string CallbackScheme = "wordsuggestor-ocr";
    public const string CallbackUri = "wordsuggestor-ocr://callback";

    private static readonly string[] CallbackDirectories =
    [
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WordSuggestor",
            "ocr-callbacks"),
        Path.Combine(
            Path.GetTempPath(),
            "WordSuggestor",
            "ocr-callbacks")
    ];

    public static bool TryPersistStartupCallback(IReadOnlyList<string> args)
    {
        EmitDiagnostic($"Startup callback probe: args={args.Count}");
        for (var index = 0; index < args.Count; index++)
        {
            var arg = ReconstructPossibleCallbackArg(args, index);
            if (!Uri.TryCreate(arg, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, CallbackScheme, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            EmitDiagnostic("Startup callback URI detected.");
            var callback = ParseCallback(uri);
            if (callback is null)
            {
                EmitDiagnostic("Startup callback ignored: missing correlation id.");
                return true;
            }

            if (!TryWriteCallback(callback))
            {
                EmitDiagnostic(
                    $"Startup callback persistence failed: correlation={callback.CorrelationId} code={callback.Code} tokenPresent={callback.Token is not null}");
            }

            return true;
        }

        return false;
    }

    private static string ReconstructPossibleCallbackArg(IReadOnlyList<string> args, int startIndex)
    {
        var candidate = args[startIndex];
        if (!candidate.StartsWith($"{CallbackScheme}:", StringComparison.OrdinalIgnoreCase))
        {
            return candidate;
        }

        for (var index = startIndex + 1; index < args.Count; index++)
        {
            if (args[index].Contains(':', StringComparison.Ordinal) ||
                args[index].StartsWith("--", StringComparison.Ordinal))
            {
                break;
            }

            candidate += candidate.Contains('?', StringComparison.Ordinal) ? $"&{args[index]}" : $"?{args[index]}";
        }

        return candidate;
    }

    public static string ResolveCallbackPath(string correlationId) =>
        Path.Combine(ResolvePreferredCallbackDirectory(), $"{SanitizeCorrelationId(correlationId)}.callback");

    public static IReadOnlyList<string> ResolveCallbackPaths(string correlationId) =>
        ResolveCandidateCallbackPaths(correlationId).ToArray();

    public static OcrScreenClipCallback? TryReadCallback(string correlationId)
    {
        foreach (var path in ResolveCandidateCallbackPaths(correlationId))
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var raw = File.ReadAllText(path).Trim();
            return Uri.TryCreate(raw, UriKind.Absolute, out var uri)
                ? ParseCallback(uri)
                : null;
        }

        return null;
    }

    public static void DeleteCallback(string correlationId)
    {
        foreach (var path in ResolveCandidateCallbackPaths(correlationId))
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static bool TryWriteCallback(OcrScreenClipCallback callback)
    {
        foreach (var directory in CallbackDirectories)
        {
            try
            {
                Directory.CreateDirectory(directory);
                var path = Path.Combine(directory, $"{SanitizeCorrelationId(callback.CorrelationId)}.callback");
                File.WriteAllText(path, callback.RawUri);
                EmitDiagnostic(
                    $"Startup callback persisted: correlation={callback.CorrelationId} code={callback.Code} tokenPresent={callback.Token is not null} path={path}");
                return true;
            }
            catch (IOException ex)
            {
                EmitDiagnostic($"Startup callback write failed in {directory}: {ex.GetType().Name}: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                EmitDiagnostic($"Startup callback write failed in {directory}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return false;
    }

    private static string ResolvePreferredCallbackDirectory()
    {
        foreach (var directory in CallbackDirectories)
        {
            if (CanWriteToDirectory(directory))
            {
                return directory;
            }
        }

        return CallbackDirectories[^1];
    }

    private static IEnumerable<string> ResolveCandidateCallbackPaths(string correlationId)
    {
        var fileName = $"{SanitizeCorrelationId(correlationId)}.callback";
        return CallbackDirectories
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(directory => Path.Combine(directory, fileName));
    }

    private static bool CanWriteToDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            var probePath = Path.Combine(directory, $"probe-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probePath, "probe");
            File.Delete(probePath);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static OcrScreenClipCallback? ParseCallback(Uri uri)
    {
        var query = ParseQuery(uri.Query);
        var correlationId = FirstNonEmpty(
            query.GetValueOrDefault("x-request-correlation-id"),
            query.GetValueOrDefault("request-correlation-id"));

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            return null;
        }

        var code = int.TryParse(query.GetValueOrDefault("code"), out var parsedCode)
            ? parsedCode
            : 0;

        var token = FirstNonEmpty(
            query.GetValueOrDefault("token"),
            query.GetValueOrDefault("file-access-token"),
            query.GetValueOrDefault("sharedAccessToken"),
            query.GetValueOrDefault("shared-access-token"),
            query.GetValueOrDefault("sharedStorageToken"),
            query.GetValueOrDefault("shared-storage-token"));

        EmitDiagnostic(
            $"Parsed callback query: keys={string.Join(",", query.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))} tokenPresent={!string.IsNullOrWhiteSpace(token)} tokenLength={token?.Length ?? 0}");

        return new OcrScreenClipCallback(
            correlationId,
            code,
            query.GetValueOrDefault("reason"),
            token,
            uri.ToString());
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = query.TrimStart('?');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return values;
        }

        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            var key = Uri.UnescapeDataString(pair[0].Replace('+', ' '));
            var value = pair.Length > 1
                ? Uri.UnescapeDataString(pair[1].Replace('+', ' '))
                : string.Empty;
            values[key] = value;
        }

        return values;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string SanitizeCorrelationId(string correlationId)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(correlationId.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
    }

    private static void EmitDiagnostic(string message)
    {
        try
        {
            var diagnosticsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WordSuggestor",
                "diagnostics");
            Directory.CreateDirectory(diagnosticsDirectory);
            File.AppendAllText(
                Path.Combine(diagnosticsDirectory, "ocr-callback.log"),
                $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
