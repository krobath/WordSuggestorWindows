using System.Globalization;
using Microsoft.Win32;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public static class WindowsVoiceCatalogService
{
    public const string SapiDesktopSource = "SAPI Desktop";
    public const string OneCoreSource = "OneCore";

    private static readonly (string RootPath, string Source)[] VoiceTokenRoots =
    [
        (@"SOFTWARE\Microsoft\Speech\Voices\Tokens", SapiDesktopSource),
        (@"SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens", OneCoreSource)
    ];

    public static IReadOnlyList<TtsVoiceOption> GetInstalledVoices() =>
        VoiceTokenRoots
            .SelectMany(root => ReadVoiceTokens(root.RootPath, root.Source))
            .GroupBy(voice => voice.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(option => option.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ThenByDescending(IsOneCoreVoice)
            .ThenBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static IReadOnlyList<TtsVoiceOption> GetVoiceOptionsForLanguage(string languageCode)
    {
        var voices = GetInstalledVoices();
        var languageMatches = voices
            .Where(voice => IsLanguageMatch(voice.LanguageCode, languageCode))
            .OrderByDescending(IsOneCoreVoice)
            .ThenBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (languageMatches.Length > 0)
        {
            return languageMatches;
        }

        return voices
            .OrderByDescending(IsOneCoreVoice)
            .ThenBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(voice => voice with { IsFallback = true })
            .ToArray();
    }

    public static TtsVoiceSelection ResolveVoice(
        string languageCode,
        string? preferredVoiceId)
    {
        var voices = GetInstalledVoices();
        if (voices.Count == 0)
        {
            return new TtsVoiceSelection(
                null,
                "Der er ingen Windows-stemmer installeret for brugeren.");
        }

        if (!string.IsNullOrWhiteSpace(preferredVoiceId))
        {
            var preferred = voices.FirstOrDefault(voice =>
                string.Equals(voice.Id, preferredVoiceId, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
            {
                return new TtsVoiceSelection(preferred, null);
            }
        }

        var sameLanguage = voices
            .Where(voice => IsLanguageMatch(voice.LanguageCode, languageCode))
            .OrderByDescending(IsOneCoreVoice)
            .FirstOrDefault();
        if (sameLanguage is not null)
        {
            return new TtsVoiceSelection(sameLanguage, null);
        }

        var fallback = voices
            .OrderByDescending(IsOneCoreVoice)
            .First();
        return new TtsVoiceSelection(
            fallback with { IsFallback = true },
            $"Ingen installeret Windows-stemme matcher {languageCode}; bruger {fallback.DisplayName} ({fallback.LanguageCode}, {fallback.Source}).");
    }

    public static bool HasLanguageVoice(string languageCode) =>
        GetInstalledVoices().Any(voice => IsLanguageMatch(voice.LanguageCode, languageCode));

    public static TtsVoiceSelection ResolveVoiceBySource(
        string languageCode,
        string? preferredVoiceId,
        string source)
    {
        var voices = GetInstalledVoices()
            .Where(voice => string.Equals(voice.Source, source, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (voices.Length == 0)
        {
            return new TtsVoiceSelection(
                null,
                $"Der er ingen installerede {source}-stemmer for Windows-brugeren.");
        }

        if (!string.IsNullOrWhiteSpace(preferredVoiceId))
        {
            var preferred = voices.FirstOrDefault(voice =>
                string.Equals(voice.Id, preferredVoiceId, StringComparison.OrdinalIgnoreCase));
            if (preferred is not null)
            {
                return new TtsVoiceSelection(preferred, null);
            }
        }

        var sameLanguage = voices.FirstOrDefault(voice => IsLanguageMatch(voice.LanguageCode, languageCode));
        if (sameLanguage is not null)
        {
            return new TtsVoiceSelection(sameLanguage, null);
        }

        var fallback = voices.First();
        return new TtsVoiceSelection(
            fallback with { IsFallback = true },
            $"Ingen installeret {source}-stemme matcher {languageCode}; bruger {fallback.DisplayName} ({fallback.LanguageCode}).");
    }

    private static IEnumerable<TtsVoiceOption> ReadVoiceTokens(string rootPath, string source)
    {
        using var root = Registry.LocalMachine.OpenSubKey(rootPath);
        if (root is null)
        {
            yield break;
        }

        foreach (var tokenName in root.GetSubKeyNames())
        {
            using var token = root.OpenSubKey(tokenName);
            using var attributes = token?.OpenSubKey("Attributes");
            if (token is null || attributes is null)
            {
                continue;
            }

            var displayName = Convert.ToString(token.GetValue(string.Empty), CultureInfo.InvariantCulture);
            var language = Convert.ToString(attributes.GetValue("Language"), CultureInfo.InvariantCulture);
            var languageCode = ResolveLanguageCode(language);
            var tokenId = $@"HKEY_LOCAL_MACHINE\{rootPath}\{tokenName}";

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                yield return new TtsVoiceOption(tokenId, displayName, languageCode, source);
            }
        }
    }

    private static string ResolveLanguageCode(string? languageAttribute)
    {
        if (string.IsNullOrWhiteSpace(languageAttribute))
        {
            return CultureInfo.CurrentUICulture.Name;
        }

        var firstLanguage = languageAttribute
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (firstLanguage is null)
        {
            return CultureInfo.CurrentUICulture.Name;
        }

        try
        {
            var lcid = int.Parse(firstLanguage, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return CultureInfo.GetCultureInfo(lcid).Name;
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.CurrentUICulture.Name;
        }
        catch (FormatException)
        {
            return CultureInfo.CurrentUICulture.Name;
        }
    }

    private static bool IsLanguageMatch(string installedLanguageCode, string requestedLanguageCode)
    {
        if (string.Equals(installedLanguageCode, requestedLanguageCode, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var installedPrefix = installedLanguageCode.Split('-')[0];
        var requestedPrefix = requestedLanguageCode.Split('-')[0];
        return string.Equals(installedPrefix, requestedPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOneCoreVoice(TtsVoiceOption voice) =>
        string.Equals(voice.Source, OneCoreSource, StringComparison.OrdinalIgnoreCase);
}

public sealed record TtsVoiceSelection(TtsVoiceOption? Voice, string? FallbackReason);
