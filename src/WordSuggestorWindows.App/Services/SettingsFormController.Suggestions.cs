using System.Windows;
using System.Windows.Controls;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

internal sealed partial class SettingsFormController
{
    private long _suggestionCapabilityRevision;
    private readonly HashSet<string> _editedDomainSelectors = [];
    private static readonly string[] SuggestionCombos = ["SuggestionDensity", "SuggestionSpacing", "OrdinaryShortcut", "PrimaryShortcut", "SecondaryShortcut", "PrimaryDomain", "SecondaryDomain"];
    private static readonly string[] SuggestionChecks = ["SpeakOnHover", "WordForms", "LivePeriodAssist", "ContextBased", "ConfusionSet", "Phonetic", "Misspelling", "Synonym", "EditorWrap"];
    private void AttachSuggestionControls()
    {
        foreach (var name in SuggestionCombos)
        {
            if (Optional<ComboBox>(name) is not { } combo) continue;
            SelectionChangedEventHandler handler = (_, _) =>
            {
                if (_loading) return;
                if (name is "PrimaryDomain" or "SecondaryDomain") _editedDomainSelectors.Add(name);
                if (name.EndsWith("Shortcut")) SwapShortcutConflict(name, combo.SelectedValue as string);
                CommitLive();
            };
            combo.SelectionChanged += handler; _unsubscribe.Add(() => combo.SelectionChanged -= handler);
        }
        foreach (var name in SuggestionChecks)
        {
            if (Optional<CheckBox>(name) is not { } check) continue;
            RoutedEventHandler handler = (_, _) => CommitLive();
            check.Checked += handler; check.Unchecked += handler;
            _unsubscribe.Add(() => { check.Checked -= handler; check.Unchecked -= handler; });
        }
        foreach (var name in new[] { "SuggestionResponsiveness", "SuggestionIdleFade" })
        {
            if (Optional<Slider>(name) is not { } slider) continue;
            RoutedPropertyChangedEventHandler<double> handler = (_, _) => CommitLive();
            slider.ValueChanged += handler; _unsubscribe.Add(() => slider.ValueChanged -= handler);
        }
    }
    private void SwapShortcutConflict(string changed, string? value)
    {
        if (value is null) return;
        var p = _settings.Suggestions;
        var previous = changed switch { "OrdinaryShortcut" => p.OrdinaryShortcut, "PrimaryShortcut" => p.PrimaryShortcut, _ => p.SecondaryShortcut };
        _loading = true;
        try
        {
            foreach (var name in new[] { "OrdinaryShortcut", "PrimaryShortcut", "SecondaryShortcut" })
                if (name != changed && Optional<ComboBox>(name) is { } other && other.SelectedValue as string == value)
                    other.SelectedValue = previous;
        }
        finally { _loading = false; }
    }
    private void LoadSuggestionControls()
    {
        if (Optional<ComboBox>("SuggestionDensity") is null) return; // legacy preserves these fields
        _editedDomainSelectors.Clear();
        var p = _settings.Suggestions;
        void Choices(string name, string[] values, string[] keys, string selected)
        {
            var control = Find<ComboBox>(name);
            control.ItemsSource = values.Select((v, i) => new SettingsOption(v, SettingsText.Get(keys[i]))).ToArray();
            control.SelectedValue = selected;
        }
        Choices("SuggestionDensity", ["wordOnly", "wordAndOrigin", "fullDetails"], ["DensityWord", "DensityOrigin", "DensityFull"], p.Density);
        Choices("SuggestionSpacing", ["smart", "always", "never"], ["SpacingSmart", "SpacingAlways", "SpacingNever"], p.Spacing);
        foreach (var (name, selected) in new[] { ("OrdinaryShortcut", p.OrdinaryShortcut), ("PrimaryShortcut", p.PrimaryShortcut), ("SecondaryShortcut", p.SecondaryShortcut) })
        {
            var c = Find<ComboBox>(name);
            c.ItemsSource = new[] { new SettingsOption("ctrl", "Ctrl + 1–0"), new SettingsOption("ctrlShift", "Ctrl + Shift + 1–0"), new SettingsOption("alt", "Alt + 1–0") };
            c.SelectedValue = selected;
        }
        var values = new[] { p.SpeakOnHover, p.WordForms, p.LivePeriodAssist, p.ContextBased, p.ConfusionSet, p.Phonetic, p.Misspelling, p.Synonym, p.EditorWrap };
        for (var i = 0; i < values.Length; i++) Find<CheckBox>(SuggestionChecks[i]).IsChecked = values[i];
        Find<Slider>("SuggestionResponsiveness").Value = p.Responsiveness;
        Find<Slider>("SuggestionIdleFade").Value = p.IdleFadeSeconds;
        _ = RefreshSuggestionCapabilitiesAsync();
    }
    private void ReadSuggestionControls()
    {
        if (Optional<ComboBox>("SuggestionDensity") is null) return;
        var p = _settings.Suggestions;
        string Value(string name, string fallback) => Find<ComboBox>(name).SelectedValue as string ?? fallback;
        bool Check(string name) => Find<CheckBox>(name).IsChecked == true;
        var primary = new Dictionary<string, string>(p.PrimaryDomains); var secondary = new Dictionary<string, string>(p.SecondaryDomains);
        var language = _settings.SelectedLanguageCode;
        foreach (var (name, map) in new[] { ("PrimaryDomain", primary), ("SecondaryDomain", secondary) })
        {
            var c = Find<ComboBox>(name);
            if (!c.IsEnabled || !_editedDomainSelectors.Contains(name)) continue;
            if (c.SelectedValue is string id && id.Length > 0) map[language] = id; else map.Remove(language);
        }
        if (!primary.TryGetValue(language, out var selected) || secondary.GetValueOrDefault(language) == selected) secondary.Remove(language);
        _settings.Suggestions = p with
        {
            Density = Value("SuggestionDensity", p.Density), Spacing = Value("SuggestionSpacing", p.Spacing),
            Responsiveness = (int)Find<Slider>("SuggestionResponsiveness").Value, IdleFadeSeconds = (int)Find<Slider>("SuggestionIdleFade").Value,
            OrdinaryShortcut = Value("OrdinaryShortcut", p.OrdinaryShortcut), PrimaryShortcut = Value("PrimaryShortcut", p.PrimaryShortcut), SecondaryShortcut = Value("SecondaryShortcut", p.SecondaryShortcut),
            SpeakOnHover = Check("SpeakOnHover"), WordForms = Check("WordForms"), LivePeriodAssist = Check("LivePeriodAssist"),
            ContextBased = Check("ContextBased"), ConfusionSet = Check("ConfusionSet"), Phonetic = Check("Phonetic"), Misspelling = Check("Misspelling"), Synonym = Check("Synonym"), EditorWrap = Check("EditorWrap"),
            PrimaryDomains = primary, SecondaryDomains = secondary
        };
    }
    private async Task RefreshSuggestionCapabilitiesAsync()
    {
        var revision = ++_suggestionCapabilityRevision;
        var language = _settings.SelectedLanguageCode;
        var catalog = _viewModel.ResolveDomainCatalog();
        var caps = CoreCapabilities.Legacy;
        IReadOnlyList<CoreDomain> domains = [];
        try
        {
            if (_viewModel.CoreSettings is { } adapter)
            {
                caps = await adapter.GetCapabilitiesAsync(catalog, _updateCancellation.Token);
                if (caps.DomainCatalog && catalog is not null)
                    domains = (await adapter.GetDomainsAsync(catalog, _viewModel.SuggestionScope, _updateCancellation.Token)).Value;
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException or System.IO.IOException or NotSupportedException) { }
        if (_disposed || revision != _suggestionCapabilityRevision || language != _settings.SelectedLanguageCode) return;
        var oldLoading = _loading; _loading = true;
        try
        {
            var enabled = new[] { ("SuggestionSpacing", caps.CommitPlans), ("WordForms", caps.WordFamilies || caps.MorphologicalAnalyses), ("LivePeriodAssist", caps.LivePeriodAssist),
                ("ContextBased", caps.ContextReranking), ("ConfusionSet", caps.SourcePreferences), ("Phonetic", caps.Phonetic), ("Misspelling", caps.Misspellings), ("Synonym", caps.Synonyms) };
            foreach (var (name, available) in enabled)
                if (Optional<Control>(name) is { } c) { c.IsEnabled = available; c.ToolTip = available ? null : SettingsText.Get("SuggestionUnavailable"); }
            Domain.IsEnabled = domains.Count > 0;
            Domain.ToolTip = Domain.IsEnabled ? null : SettingsText.Get("SuggestionUnavailable");
            if (Optional<ComboBox>("PrimaryDomain") is { } primary && Optional<ComboBox>("SecondaryDomain") is { } secondary)
            {
                var p = _settings.Suggestions;
                var selected = p.PrimaryDomains.GetValueOrDefault(language, "");
                primary.ItemsSource = new[] { new SettingsOption("", SettingsText.Get("DomainNone")) }.Concat(domains.Select(d => new SettingsOption(d.Id, d.Title))).ToArray();
                primary.SelectedValue = selected;
                secondary.ItemsSource = new[] { new SettingsOption("", SettingsText.Get("DomainNone")) }.Concat(domains.Where(d => d.Id != selected).Select(d => new SettingsOption(d.Id, d.Title))).ToArray();
                secondary.SelectedValue = p.SecondaryDomains.GetValueOrDefault(language, "");
                primary.IsEnabled = Domain.IsEnabled && _settings.IsDomainListsEnabled;
                secondary.IsEnabled = primary.IsEnabled && domains.Any(d => d.Id == selected);
            }
            if (Optional<TextBlock>("SuggestionCapabilityStatus") is { } status) status.Text = SettingsText.Get("SuggestionReady") + " " + SettingsText.Get("SuggestionsNote");
        }
        finally { _loading = oldLoading; }
    }
}
