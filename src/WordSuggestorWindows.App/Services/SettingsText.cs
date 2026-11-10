using System.Globalization;
using System.Text.Json;
using System.Windows;

namespace WordSuggestorWindows.App.Services;

// Settings owns its UI copy; writing language and OS speech identities never
// select translations. Native Windows dialogs continue using the OS language.
public static class SettingsText
{
    private static readonly Dictionary<string, string> Danish = Read("da");
    private static readonly Dictionary<string, string> English = Read("en");
    public static string ResolvedLocale { get; private set; } = "da-DK";
    public static string Preference { get; private set; } = "system_default";
    public static event EventHandler? Changed;
    public static IReadOnlyCollection<string> Keys => Danish.Keys;

    private static Dictionary<string, string> Read(string language)
    {
        using var stream = typeof(SettingsText).Assembly.GetManifestResourceStream($"WordSuggestorWindows.App.Localization.Settings.{language}.json")
            ?? throw new InvalidOperationException("Missing Settings translations.");
        return JsonSerializer.Deserialize<Dictionary<string, string>>(stream)!;
    }

    public static string ResolveLocale(string preference, string systemLocale) => preference switch
    {
        "da-DK" => "da-DK",
        "en-GB" => "en-GB",
        _ => systemLocale.Replace('_', '-').Split('-')[0].Equals("da", StringComparison.OrdinalIgnoreCase) ? "da-DK" : "en-GB"
    };

    public static string Get(string key) => (ResolvedLocale == "da-DK" ? Danish : English)[key];
    public static string Format(string key, params object?[] values) => string.Format(CultureInfo.CurrentCulture, Get(key), values);
    public static bool CatalogsMatch => Danish.Keys.Order().SequenceEqual(English.Keys.Order()) && Danish.Values.All(v => !string.IsNullOrWhiteSpace(v)) && English.Values.All(v => !string.IsNullOrWhiteSpace(v));

    public static void ApplyPreference(string preference)
    {
        Preference = preference;
        ResolvedLocale = ResolveLocale(preference, CultureInfo.CurrentUICulture.Name);
        if (Application.Current is { } application)
        {
            foreach (var key in Keys) application.Resources["WS.Settings." + key] = Get(key);
        }
        Changed?.Invoke(null, EventArgs.Empty);
    }
}
