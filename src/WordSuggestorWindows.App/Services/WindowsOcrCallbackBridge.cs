using System.IO;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public static class WindowsOcrCallbackBridge
{
    public const string CallbackScheme = "wordsuggestor-ocr";
    public const string CallbackUri = "wordsuggestor-ocr://callback";

    private static readonly string CallbackDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WordSuggestor",
        "ocr-callbacks");

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

            Directory.CreateDirectory(CallbackDirectory);
            File.WriteAllText(ResolveCallbackPath(callback.CorrelationId), callback.RawUri);
            EmitDiagnostic($"Startup callback persisted: correlation={callback.CorrelationId} code={callback.Code}");
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
        Path.Combine(CallbackDirectory, $"{SanitizeCorrelationId(correlationId)}.uri");

    public static OcrScreenClipCallback? TryReadCallback(string correlationId)
    {
        var path = ResolveCallbackPath(correlationId);
        if (!File.Exists(path))
        {
            return null;
        }

        var raw = File.ReadAllText(path).Trim();
        return Uri.TryCreate(raw, UriKind.Absolute, out var uri)
            ? ParseCallback(uri)
            : null;
    }

    public static void DeleteCallback(string correlationId)
    {
        try
        {
            File.Delete(ResolveCallbackPath(correlationId));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
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

        return new OcrScreenClipCallback(
            correlationId,
            code,
            query.GetValueOrDefault("reason"),
            query.GetValueOrDefault("token"),
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
