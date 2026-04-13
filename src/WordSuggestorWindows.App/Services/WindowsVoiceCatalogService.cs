using System.Globalization;
using Microsoft.Win32;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public static class WindowsVoiceCatalogService
{
    private static readonly string[] SapiVoiceTokenRoots =
    [
        @"SOFTWARE\Microsoft\Speech\Voices\Tokens"
    ];

    public static IReadOnlyList<TtsVoiceOption> GetInstalledSapiVoices() =>
        SapiVoiceTokenRoots
            .SelectMany(ReadVoiceTokens)
            .GroupBy(voice => voice.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(option => option.LanguageCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(option => option.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public static IReadOnlyList<TtsVoiceOption> GetVoiceOptionsForLanguage(string languageCode)
    {
        var voices = GetInstalledSapiVoices();
        var languageMatches = voices
            .Where(voice => IsLanguageMatch(voice.LanguageCode, languageCode))
            .ToArray();

        if (languageMatches.Length > 0)
        {
            return languageMatches;
        }

        return voices
            .Select(voice => voice with { IsFallback = true })
            .ToArray();
    }

    public static TtsVoiceSelection ResolveVoice(
        string languageCode,
        string? preferredVoiceId)
    {
        var voices = GetInstalledSapiVoices();
        if (voices.Count == 0)
        {
            return new TtsVoiceSelection(
                null,
                "Der er ingen SAPI Desktop-stemmer installeret for Windows-brugeren.");
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
            $"Ingen installeret SAPI-stemme matcher {languageCode}; bruger {fallback.DisplayName} ({fallback.LanguageCode}).");
    }

    public static bool HasLanguageVoice(string languageCode) =>
        GetInstalledSapiVoices().Any(voice => IsLanguageMatch(voice.LanguageCode, languageCode));

    private static IEnumerable<TtsVoiceOption> ReadVoiceTokens(string rootPath)
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
                yield return new TtsVoiceOption(tokenId, displayName, languageCode, "SAPI Desktop");
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
}

public sealed record TtsVoiceSelection(TtsVoiceOption? Voice, string? FallbackReason);
