using System.Windows;
using System.Windows.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App.Services;

internal sealed class SettingsFormController : IDisposable
{
    private readonly MainWindowViewModel _viewModel;
    private readonly Window _host;
    private readonly bool _live;
    private AppSettingsSnapshot _original;
    private AppSettingsSnapshot _settings;
    private bool _loading = true;
    private bool _disposed;
    private bool _refreshingVoices;
    private string? _voiceLanguageKey;
    private readonly HashSet<string> _editedVoiceLanguages = [];
    private readonly List<Action> _unsubscribe = [];
    private readonly WindowsUpdateService _updates = new();
    private readonly CancellationTokenSource _updateCancellation = new();
    private UpdateCheckResult? _updateResult;
    private bool _checkingUpdates;
    private string? _statusKey;

    public SettingsFormController(Window host, MainWindowViewModel viewModel, bool live = false)
    {
        _host = host; _viewModel = viewModel; _live = live;
        _original = viewModel.LoadSettingsSnapshot().Clone(); _settings = _original.Clone();
        InitializeOptionSources(); LoadControls(); RenderStatus();
        _loading = false;
        Language.SelectionChanged += OnLanguageChanged;
        Voice.SelectionChanged += OnVoiceChanged;
        SystemSpeech.Checked += OnSpeechChanged; SystemSpeech.Unchecked += OnSpeechChanged;
        Speed.ValueChanged += OnSpeedChanged;
        _unsubscribe.Add(() => { Language.SelectionChanged -= OnLanguageChanged; Voice.SelectionChanged -= OnVoiceChanged;
            SystemSpeech.Checked -= OnSpeechChanged; SystemSpeech.Unchecked -= OnSpeechChanged; Speed.ValueChanged -= OnSpeedChanged; });
        if (live)
        {
            foreach (var combo in new[] { Optional<ComboBox>("InterfaceLanguageComboBox"), SpeechLanguage, Strategy, Highlight })
            {
                if (combo is null) continue;
                SelectionChangedEventHandler handler = (_, _) => CommitLive();
                combo.SelectionChanged += handler; _unsubscribe.Add(() => combo.SelectionChanged -= handler);
            }
            foreach (var check in new[] { Optional<CheckBox>("FollowCaretOptionCheckBox"), Global, Domain, Analysis, Colouring, Semantic, Punctuation, Errors, Examples, Performance, AppDebug, PanelDebug, CoreDebug })
            {
                if (check is null) continue;
                RoutedEventHandler handler = (_, _) => CommitLive();
                check.Checked += handler; check.Unchecked += handler;
                _unsubscribe.Add(() => { check.Checked -= handler; check.Unchecked -= handler; });
            }
        }
        SettingsText.Changed += OnCopyChanged;
        viewModel.SettingsChanged += OnStoredSettingsChanged;
        _unsubscribe.Add(() => { SettingsText.Changed -= OnCopyChanged; viewModel.SettingsChanged -= OnStoredSettingsChanged; });
        _host.Closed += (_, _) => Dispose();
        if (!viewModel.CanWriteSettings) SetStatus("UnavailableData");
    }

    private void InitializeOptionSources()
    {
        Language.ItemsSource = _viewModel.LanguageOptions.Select(l => new SettingsOption(l.LanguageCode,
            l.IsPackAvailable ? LanguageName(l) : SettingsText.Format("MissingPack", LanguageName(l)), l.IsPackAvailable)).ToArray();
        if (Optional<ComboBox>("InterfaceLanguageComboBox") is { } ui)
            ui.ItemsSource = new[] { Option("system_default", "FollowWindows"), Option("da-DK", "Danish"), Option("en-GB", "English") };
        if (Optional<ComboBox>("PlacementModeComboBox") is { } placement)
            placement.ItemsSource = new[] { Option("followCaret", "FollowCaret"), Option("static", "Static") };
        SpeechLanguage.ItemsSource = new[] { Option("useSelectedLanguage", "UseSelectedLanguage"), Option("autoDetect", "AutoLanguage") };
        Strategy.ItemsSource = new[] { Option("none", "None"), Option("word", "ReadWord"), Option("sentence", "ReadSentence"), Option("paragraph", "ReadParagraph") };
        Highlight.ItemsSource = new[] { Option("none", "None"), Option("word", "Word"), Option("sentence", "Sentence") };
    }

    private static string LanguageName(LanguageOption language) => language.LanguageCode.ToLowerInvariant() switch
    { "da-dk" => SettingsText.Get("Danish"), "en-us" => SettingsText.Get("English"), "de-de" => SettingsText.Get("German"), _ => language.LanguageCode };
    private static SettingsOption Option(string value, string key) => new(value, SettingsText.Get(key));

    private void LoadControls()
    {
        Language.SelectedValue = _settings.SelectedLanguageCode;
        // Unknown/missing writing-language intentions are retained, not replaced
        // by a displayed fallback language on an unrelated save.
        if (Language.SelectedIndex < 0)
        {
            var choices = ((IEnumerable<SettingsOption>)Language.ItemsSource).ToList();
            choices.Add(new(_settings.SelectedLanguageCode, SettingsText.Format("UnavailableLanguage", _settings.SelectedLanguageCode), false));
            Language.ItemsSource = choices; Language.SelectedValue = _settings.SelectedLanguageCode;
        }
        if (Optional<ComboBox>("InterfaceLanguageComboBox") is { } ui) ui.SelectedValue = _settings.InterfaceLocalePreference;
        if (Optional<CheckBox>("FollowCaretOptionCheckBox") is { } allowed) allowed.IsChecked = _settings.FollowCaretOptionEnabled;
        if (Optional<ComboBox>("PlacementModeComboBox") is { } placement)
        { placement.SelectedValue = _settings.SuggestionPlacementMode; placement.IsEnabled = _viewModel.FollowCaretOptionEnabled; }
        SpeechLanguage.SelectedValue = _settings.SpeechLanguageMode;
        Global.IsChecked = _settings.IsGlobalCaptureEnabled; Domain.IsChecked = _settings.IsDomainListsEnabled;
        SystemSpeech.IsChecked = _settings.UseSystemSpeechSettings; Speed.Value = SpeechSpeedPolicy.Percentage(_settings.ReadingSpeedDelta);
        Strategy.SelectedValue = _settings.ReadingStrategy; Highlight.SelectedValue = _settings.ReadingHighlightMode;
        Analysis.IsChecked = _settings.IsTextAnalyzerEnabled; Colouring.IsChecked = _settings.IsTextAnalyzerColoringEnabled;
        Semantic.IsChecked = _settings.IsSemanticDiagnosticsEnabled; Punctuation.IsChecked = _settings.IsPunctuationDiagnosticsEnabled;
        Errors.IsChecked = _settings.IsErrorTrackingEnabled; Examples.IsChecked = _settings.StoreSentenceExamples;
        Performance.IsChecked = _settings.IsPerformanceInstrumentationEnabled; AppDebug.IsChecked = _settings.IsAppDebugLoggingEnabled;
        PanelDebug.IsChecked = _settings.IsPlacementDebugLoggingEnabled; CoreDebug.IsChecked = _settings.IsCoreDebugLoggingEnabled;
        RefreshSpeechControls();
    }

    private void ReadControls()
    {
        _settings.SelectedLanguageCode = Language.SelectedValue as string ?? _settings.SelectedLanguageCode;
        if (Optional<ComboBox>("InterfaceLanguageComboBox") is { } ui) _settings.InterfaceLocalePreference = ui.SelectedValue as string ?? _settings.InterfaceLocalePreference;
        if (Optional<CheckBox>("FollowCaretOptionCheckBox") is { } allowed) _settings.FollowCaretOptionEnabled = allowed.IsChecked == true;
        if (Optional<ComboBox>("PlacementModeComboBox") is { } placement) _settings.SuggestionPlacementMode = placement.SelectedValue as string ?? _settings.SuggestionPlacementMode;
        _settings.SpeechLanguageMode = SpeechLanguage.SelectedValue as string ?? _settings.SpeechLanguageMode;
        _settings.IsGlobalCaptureEnabled = Global.IsChecked == true; _settings.IsDomainListsEnabled = Domain.IsChecked == true;
        _settings.UseSystemSpeechSettings = SystemSpeech.IsChecked == true; _settings.ReadingSpeedDelta = SpeechSpeedPolicy.Normalize((Speed.Value - 100) / 100);
        _settings.ReadingStrategy = Strategy.SelectedValue as string ?? _settings.ReadingStrategy;
        _settings.ReadingHighlightMode = Highlight.SelectedValue as string ?? _settings.ReadingHighlightMode;
        SaveVoiceOverride();
        _settings.IsTextAnalyzerEnabled = Analysis.IsChecked == true; _settings.IsTextAnalyzerColoringEnabled = Colouring.IsChecked == true;
        _settings.IsSemanticDiagnosticsEnabled = Semantic.IsChecked == true; _settings.IsPunctuationDiagnosticsEnabled = Punctuation.IsChecked == true;
        _settings.IsErrorTrackingEnabled = Errors.IsChecked == true; _settings.StoreSentenceExamples = Examples.IsChecked == true;
        _settings.IsPerformanceInstrumentationEnabled = Performance.IsChecked == true; _settings.IsAppDebugLoggingEnabled = AppDebug.IsChecked == true;
        _settings.IsPlacementDebugLoggingEnabled = PanelDebug.IsChecked == true; _settings.IsCoreDebugLoggingEnabled = CoreDebug.IsChecked == true;
    }

    private void CommitLive()
    {
        if (_loading || !_live || _disposed) return;
        ReadControls();
        if (_viewModel.TryApplySettingsChanges(_original, _settings)) SetStatus("Saved");
        else
        {
            // Failed live changes are neither queued nor secretly applied. Show
            // the effective last-known settings and retain a visible error.
            _settings = _original.Clone(); _editedVoiceLanguages.Clear();
            _loading = true; try { LoadControls(); } finally { _loading = false; }
            Find<TextBlock>("SettingsSaveStatus").Text = _viewModel.StatusMessage;
            _statusKey = null;
        }
    }

    public void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        ReadControls();
        if (!_viewModel.TryApplySettingsChanges(_original, _settings))
        { Find<TextBlock>("SettingsSaveStatus").Text = _viewModel.StatusMessage; return; }
        _host.Close();
    }
    public void CancelButton_OnClick(object sender, RoutedEventArgs e) => _host.Close();

    private void OnStoredSettingsChanged(object? sender, AppSettingsSnapshot snapshot)
    {
        if (_disposed) return;
        if (!_live)
        {
            // Keep the legacy draft/baseline for three-way conflict detection.
            if (Optional<ComboBox>("PlacementModeComboBox") is { } placement) placement.IsEnabled = snapshot.FollowCaretOptionEnabled;
            return;
        }
        _original = snapshot.Clone(); _settings = snapshot.Clone(); _editedVoiceLanguages.Clear();
        _loading = true; try { LoadControls(); } finally { _loading = false; }
    }

    private void OnCopyChanged(object? sender, EventArgs e)
    {
        if (_disposed) return;
        // Changing translations must not save, clear a legacy draft or reapply
        // a voice choice while rebuilding the option presentation.
        _loading = true;
        try { ReadControls(); InitializeOptionSources(); LoadControls(); RenderStatus(); }
        finally { _loading = false; }
    }
    private void SetStatus(string key) { _statusKey = key; Find<TextBlock>("SettingsSaveStatus").Text = SettingsText.Get(key); }
    private void RenderStatus()
    {
        Find<TextBlock>("VersionText").Text = _updates.VersionLabel;
        Find<TextBlock>("UpdateStatusText").Text = _checkingUpdates ? SettingsText.Get("UpdateChecking") : _updateResult is { } result
            ? SettingsText.Format("Update." + result.Code, result.AvailableVersion)
            : SettingsText.Get(_updates.IsProductionInstalled ? "UpdateInstalled" : "UpdateDevelopment");
        if (_statusKey is not null) SetStatus(_statusKey);
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    { if (_loading) return; RefreshSpeechControls(); CommitLive(); }
    private void OnSpeechChanged(object sender, RoutedEventArgs e)
    { if (_loading) return; RefreshSpeechControls(); CommitLive(); }
    private void OnSpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    { Find<TextBlock>("ReadingSpeedValueText").Text = SpeedLabel(); UseAppSpeechForExplicitChange(); CommitLive(); }
    private void OnVoiceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || _refreshingVoices || _voiceLanguageKey is null) return;
        _editedVoiceLanguages.Add(_voiceLanguageKey); UseAppSpeechForExplicitChange(); CommitLive();
    }

    private void UseAppSpeechForExplicitChange()
    {
        if (_loading || !_live) return;
        // Explicit edits in the primary surface choose app control; preserve the
        // old system preference until the user actually changes voice or tempo.
        _loading = true; try { SystemSpeech.IsChecked = false; } finally { _loading = false; }
    }

    private void RefreshSpeechControls()
    {
        if (!_loading) SaveVoiceOverride();
        var language = Language.SelectedValue as string ?? _settings.SelectedLanguageCode;
        _voiceLanguageKey = language.ToLowerInvariant();
        var voices = WindowsVoiceCatalogService.GetVoiceOptionsForLanguage(language);
        var options = new List<SettingsOption> { Option("", "AutomaticVoice") };
        options.AddRange(voices.Select(v => new SettingsOption(v.Id, v.DisplayLabel)));
        _refreshingVoices = true;
        try
        {
            Voice.ItemsSource = options;
            Voice.SelectedValue = _settings.SystemVoiceIdOverrideByLanguage.TryGetValue(_voiceLanguageKey, out var saved) && options.Any(o => o.Value.Equals(saved, StringComparison.OrdinalIgnoreCase)) ? saved : "";
        }
        finally { _refreshingVoices = false; }
        Voice.IsEnabled = (_live || SystemSpeech.IsChecked != true) && options.Count > 1;
        Speed.IsEnabled = _live || SystemSpeech.IsChecked != true;
        var unavailableOneCore = voices.Any(v => v.Source == WindowsVoiceCatalogService.OneCoreSource && !v.IsFallback) && !WindowsTextToSpeechService.GetOneCoreRuntimeStatus().IsAvailable;
        Find<TextBlock>("VoiceStatusTextBlock").Text = SettingsText.Format(unavailableOneCore ? "VoiceRuntimeUnavailable" : WindowsVoiceCatalogService.HasLanguageVoice(language) ? "VoiceAvailable" : "VoiceMissing", language);
        if (_live && SystemSpeech.IsChecked == true) Find<TextBlock>("VoiceStatusTextBlock").Text += " " + SettingsText.Get("SystemSpeechOverrideNotice");
        Find<TextBlock>("ReadingSpeedValueText").Text = SpeedLabel();
    }

    private string SpeedLabel() => _live && SystemSpeech.IsChecked == true
        ? SettingsText.Format("SystemTempoValue", SpeechSpeedPolicy.Percentage((Speed.Value - 100) / 100))
        : $"{SpeechSpeedPolicy.Percentage((Speed.Value - 100) / 100)}%";

    private void SaveVoiceOverride()
    {
        if (_voiceLanguageKey is null || !_editedVoiceLanguages.Contains(_voiceLanguageKey)) return;
        if (Voice.SelectedValue is string id && !string.IsNullOrWhiteSpace(id)) _settings.SystemVoiceIdOverrideByLanguage[_voiceLanguageKey] = id;
        else _settings.SystemVoiceIdOverrideByLanguage.Remove(_voiceLanguageKey);
    }

    public async void CheckUpdates_OnClick(object sender, RoutedEventArgs e)
    {
        var button = Find<Button>("CheckUpdatesButton"); button.IsEnabled = false;
        _checkingUpdates = true;
        Find<TextBlock>("UpdateStatusText").Text = SettingsText.Get("UpdateChecking");
        try
        {
            _updateResult = await _updates.CheckAsync(_updateCancellation.Token);
            if (_disposed) return;
            _checkingUpdates = false;
            RenderStatus(); Find<Button>("OpenUpdateButton").Visibility = _updateResult.Code == "available" ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (OperationCanceledException) { }
        finally { _checkingUpdates = false; if (!_disposed) { button.IsEnabled = true; RenderStatus(); } }
    }
    public void OpenUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        try { _updates.OpenInstaller(); }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        { Find<TextBlock>("UpdateStatusText").Text = SettingsText.Get("UpdateOpenFailed"); }
    }
    public void OpenSpeechSettingsButton_OnClick(object sender, RoutedEventArgs e) => OpenWindowsSettings("ms-settings:speech");
    public void OpenLanguageSettingsButton_OnClick(object sender, RoutedEventArgs e) => OpenWindowsSettings("ms-settings:regionlanguage");
    private static void OpenWindowsSettings(string uri)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri) { UseShellExecute = true }); }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception) { }
    }

    private T Find<T>(string name) where T : FrameworkElement => Optional<T>(name) ?? throw new InvalidOperationException("Missing settings control: " + name);
    private T? Optional<T>(string name) where T : FrameworkElement => _host.FindName(name) as T;
    private ComboBox Language => Find<ComboBox>("LanguageComboBox");
    private ComboBox SpeechLanguage => Find<ComboBox>("SpeechLanguageModeComboBox");
    private ComboBox Voice => Find<ComboBox>("SystemVoiceComboBox");
    private ComboBox Strategy => Find<ComboBox>("ReadingStrategyComboBox");
    private ComboBox Highlight => Find<ComboBox>("ReadingHighlightModeComboBox");
    private Slider Speed => Find<Slider>("ReadingSpeedSlider");
    private CheckBox Global => Find<CheckBox>("GlobalSuggestionsCheckBox");
    private CheckBox Domain => Find<CheckBox>("DomainListsCheckBox");
    private CheckBox SystemSpeech => Find<CheckBox>("UseSystemSpeechSettingsCheckBox");
    private CheckBox Analysis => Find<CheckBox>("TextAnalyzerEnabledCheckBox");
    private CheckBox Colouring => Find<CheckBox>("TextAnalyzerColoringCheckBox");
    private CheckBox Semantic => Find<CheckBox>("SemanticDiagnosticsCheckBox");
    private CheckBox Punctuation => Find<CheckBox>("PunctuationDiagnosticsCheckBox");
    private CheckBox Errors => Find<CheckBox>("ErrorTrackingEnabledCheckBox");
    private CheckBox Examples => Find<CheckBox>("StoreSentenceExamplesCheckBox");
    private CheckBox Performance => Find<CheckBox>("PerformanceInstrumentationCheckBox");
    private CheckBox AppDebug => Find<CheckBox>("AppDebugLoggingCheckBox");
    private CheckBox PanelDebug => Find<CheckBox>("PlacementDebugLoggingCheckBox");
    private CheckBox CoreDebug => Find<CheckBox>("CoreDebugLoggingCheckBox");

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var unsubscribe in _unsubscribe) unsubscribe();
        _updateCancellation.Cancel(); _updates.Dispose(); _updateCancellation.Dispose();
    }
    private sealed record SettingsOption(string Value, string Label, bool IsAvailable = true);
}
