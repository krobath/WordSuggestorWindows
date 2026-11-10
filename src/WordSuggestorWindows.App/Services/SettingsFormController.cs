using System.Windows;
using System.Windows.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App.Services;

internal sealed class SettingsFormController : IDisposable
{
    private readonly MainWindowViewModel _viewModel;
    private readonly Window _host;
    private readonly AppSettingsSnapshot _original;
    private readonly AppSettingsSnapshot _settings;
    private bool _loading = true;
    private bool _disposed;
    private string? _voiceLanguageKey;
    private bool _refreshingVoices;
    private readonly HashSet<string> _editedVoiceLanguages = [];
    private readonly WindowsUpdateService _updates = new();
    private readonly CancellationTokenSource _updateCancellation = new();

    public SettingsFormController(Window host, MainWindowViewModel viewModel)
    {
        _host = host;
        _viewModel = viewModel;
        _original = viewModel.LoadSettingsSnapshot().Clone();
        _settings = _original.Clone();
        InitializeOptionSources();
        LoadControlsFromSettings();
        VersionText.Text = _updates.VersionLabel;
        UpdateStatusText.Text = _updates.IsProductionInstalled
            ? "Automatisk kontrol ved start og i baggrunden. Du kan også kontrollere manuelt."
            : "Denne udgave er ikke installeret som den officielle MSIX-beta.";
        UseSystemSpeechSettingsCheckBox.Checked += SpeechSettings_OnChanged;
        UseSystemSpeechSettingsCheckBox.Unchecked += SpeechSettings_OnChanged;
        ReadingSpeedSlider.ValueChanged += ReadingSpeedSlider_OnValueChanged;
        SystemVoiceComboBox.SelectionChanged += OnVoiceChanged;
        _loading = false;
        _host.Closed += (_, _) => Dispose();
    }

    public async void CheckUpdates_OnClick(object sender, RoutedEventArgs e)
    {
        CheckUpdatesButton.IsEnabled = false;
        UpdateStatusText.Text = "Kontrollerer opdateringer…";
        try
        {
            var result = await _updates.CheckAsync(_updateCancellation.Token);
            UpdateStatusText.Text = result.Message;
            OpenUpdateButton.Visibility = result.Code == "available" ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (OperationCanceledException) { }
        finally { if (!_disposed) CheckUpdatesButton.IsEnabled = true; }
    }

    public void OpenUpdate_OnClick(object sender, RoutedEventArgs e)
    {
        try { _updates.OpenInstaller(); }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        { UpdateStatusText.Text = "Windows kunne ikke åbne installationsfilen. Prøv igen senere."; }
    }

    private void InitializeOptionSources()
    {
        LanguageComboBox.ItemsSource = _viewModel.LanguageOptions;
        PlacementModeComboBox.ItemsSource = new[]
        {
            new SettingsOption("followCaret", "Under tekst / følg markøren"),
            new SettingsOption("static", "Statisk placering")
        };
        SpeechLanguageModeComboBox.ItemsSource = new[]
        {
            new SettingsOption("useSelectedLanguage", "Brug valgt sprog"),
            new SettingsOption("autoDetect", "Automatisk (bruger valgt sprog i Windows-baseline)")
        };
        ReadingStrategyComboBox.ItemsSource = new[]
        {
            new SettingsOption("none", "Ingen"),
            new SettingsOption("word", "Læs ord"),
            new SettingsOption("sentence", "Læs sætning"),
            new SettingsOption("paragraph", "Læs afsnit"),
            new SettingsOption("page", "Læs side"),
            new SettingsOption("all", "Læs alt")
        };
        ReadingHighlightModeComboBox.ItemsSource = new[]
        {
            new SettingsOption("none", "Ingen"),
            new SettingsOption("word", "Ord"),
            new SettingsOption("sentence", "Sætning")
        };

        LanguageComboBox.SelectionChanged += LanguageComboBox_OnSelectionChanged;
    }

    private void LoadControlsFromSettings()
    {
        LanguageComboBox.SelectedValue = _settings.SelectedLanguageCode;
        PlacementModeComboBox.SelectedValue = _settings.SuggestionPlacementMode;
        SpeechLanguageModeComboBox.SelectedValue = _settings.SpeechLanguageMode;
        GlobalSuggestionsCheckBox.IsChecked = _settings.IsGlobalCaptureEnabled;
        DomainListsCheckBox.IsChecked = _settings.IsDomainListsEnabled;
        UseSystemSpeechSettingsCheckBox.IsChecked = _settings.UseSystemSpeechSettings;
        ReadingSpeedSlider.Value = _settings.ReadingSpeedDelta;
        ReadingStrategyComboBox.SelectedValue = _settings.ReadingStrategy;
        ReadingHighlightModeComboBox.SelectedValue = _settings.ReadingHighlightMode;
        TextAnalyzerEnabledCheckBox.IsChecked = _settings.IsTextAnalyzerEnabled;
        TextAnalyzerColoringCheckBox.IsChecked = _settings.IsTextAnalyzerColoringEnabled;
        SemanticDiagnosticsCheckBox.IsChecked = _settings.IsSemanticDiagnosticsEnabled;
        PunctuationDiagnosticsCheckBox.IsChecked = _settings.IsPunctuationDiagnosticsEnabled;
        ErrorTrackingEnabledCheckBox.IsChecked = _settings.IsErrorTrackingEnabled;
        StoreSentenceExamplesCheckBox.IsChecked = _settings.StoreSentenceExamples;
        PerformanceInstrumentationCheckBox.IsChecked = _settings.IsPerformanceInstrumentationEnabled;
        AppDebugLoggingCheckBox.IsChecked = _settings.IsAppDebugLoggingEnabled;
        PlacementDebugLoggingCheckBox.IsChecked = _settings.IsPlacementDebugLoggingEnabled;
        CoreDebugLoggingCheckBox.IsChecked = _settings.IsCoreDebugLoggingEnabled;
        RefreshSpeechControls();
    }

    public void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        _settings.SelectedLanguageCode = LanguageComboBox.SelectedValue as string ?? _settings.SelectedLanguageCode;
        _settings.SuggestionPlacementMode = PlacementModeComboBox.SelectedValue as string ?? "followCaret";
        _settings.SpeechLanguageMode = SpeechLanguageModeComboBox.SelectedValue as string ?? "useSelectedLanguage";
        _settings.IsGlobalCaptureEnabled = GlobalSuggestionsCheckBox.IsChecked == true;
        _settings.IsDomainListsEnabled = DomainListsCheckBox.IsChecked == true;
        _settings.UseSystemSpeechSettings = UseSystemSpeechSettingsCheckBox.IsChecked == true;
        _settings.ReadingSpeedDelta = ReadingSpeedSlider.Value;
        _settings.ReadingStrategy = ReadingStrategyComboBox.SelectedValue as string ?? "none";
        _settings.ReadingHighlightMode = ReadingHighlightModeComboBox.SelectedValue as string ?? "word";
        SaveVoiceOverrideForSelectedLanguage();
        _settings.IsTextAnalyzerEnabled = TextAnalyzerEnabledCheckBox.IsChecked == true;
        _settings.IsTextAnalyzerColoringEnabled = TextAnalyzerColoringCheckBox.IsChecked == true;
        _settings.IsSemanticDiagnosticsEnabled = SemanticDiagnosticsCheckBox.IsChecked == true;
        _settings.IsPunctuationDiagnosticsEnabled = PunctuationDiagnosticsCheckBox.IsChecked == true;
        _settings.IsErrorTrackingEnabled = ErrorTrackingEnabledCheckBox.IsChecked == true;
        _settings.StoreSentenceExamples = StoreSentenceExamplesCheckBox.IsChecked == true;
        _settings.IsPerformanceInstrumentationEnabled = PerformanceInstrumentationCheckBox.IsChecked == true;
        _settings.IsAppDebugLoggingEnabled = AppDebugLoggingCheckBox.IsChecked == true;
        _settings.IsPlacementDebugLoggingEnabled = PlacementDebugLoggingCheckBox.IsChecked == true;
        _settings.IsCoreDebugLoggingEnabled = CoreDebugLoggingCheckBox.IsChecked == true;

        if (!_viewModel.TryApplySettingsChanges(_original, _settings))
        {
            Find<TextBlock>("SettingsSaveStatus").Text = _viewModel.StatusMessage;
            return;
        }
        _host.Close();
    }

    public void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        _host.Close();
    }

    private void LanguageComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RefreshSpeechControls();
    }

    private void SpeechSettings_OnChanged(object sender, RoutedEventArgs e)
    {
        RefreshSpeechControls();
    }

    private void ReadingSpeedSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ReadingSpeedValueText is not null)
        {
            ReadingSpeedValueText.Text = $"{(int)((1.0 + ReadingSpeedSlider.Value) * 100)}%";
        }
    }

    public void OpenSpeechSettingsButton_OnClick(object sender, RoutedEventArgs e) =>
        OpenWindowsSettings("ms-settings:speech");

    public void OpenLanguageSettingsButton_OnClick(object sender, RoutedEventArgs e) =>
        OpenWindowsSettings("ms-settings:regionlanguage");

    private void RefreshSpeechControls()
    {
        if (SystemVoiceComboBox is null)
        {
            return;
        }

        if (!_loading) SaveVoiceOverrideForSelectedLanguage();
        var languageCode = LanguageComboBox.SelectedValue as string ?? _settings.SelectedLanguageCode;
        var languageKey = languageCode.ToLowerInvariant();
        _voiceLanguageKey = languageKey;
        var matchingVoices = WindowsVoiceCatalogService.GetVoiceOptionsForLanguage(languageCode);
        var oneCoreStatus = WindowsTextToSpeechService.GetOneCoreRuntimeStatus();
        var options = new List<SettingsOption>
        {
            new(string.Empty, "Automatisk (bedst)")
        };
        options.AddRange(matchingVoices.Select(voice => new SettingsOption(voice.Id, voice.DisplayLabel)));

        _refreshingVoices = true;
        try
        {
            SystemVoiceComboBox.ItemsSource = options;
            SystemVoiceComboBox.SelectedValue =
                _settings.SystemVoiceIdOverrideByLanguage.TryGetValue(languageKey, out var voiceId) &&
                options.Any(option => string.Equals(option.Value, voiceId, StringComparison.OrdinalIgnoreCase))
                    ? voiceId
                    : string.Empty;
        }
        finally { _refreshingVoices = false; }

        var hasLanguageVoice = WindowsVoiceCatalogService.HasLanguageVoice(languageCode);
        var oneCoreLanguageVoice = matchingVoices.FirstOrDefault(voice =>
            string.Equals(voice.Source, WindowsVoiceCatalogService.OneCoreSource, StringComparison.OrdinalIgnoreCase) &&
            !voice.IsFallback);
        var useSystemSettings = UseSystemSpeechSettingsCheckBox.IsChecked == true;
        SystemVoiceComboBox.IsEnabled = !useSystemSettings && options.Count > 1;
        ReadingSpeedSlider.IsEnabled = !useSystemSettings;
        VoiceStatusTextBlock.Text =
            oneCoreLanguageVoice is not null && !oneCoreStatus.IsAvailable
                ? $"Der findes en installeret OneCore-stemme for {languageCode} ({oneCoreLanguageVoice.DisplayName}), men OneCore-playback er ikke valideret på denne host endnu: {oneCoreStatus.Detail}"
                : hasLanguageVoice
                    ? $"Der findes en installeret Windows-stemme for {languageCode}."
                    : $"Ingen installeret Windows-stemme matcher {languageCode}. Installer en Windows-stemme for sproget, ellers bruges en fallback-stemme.";
        ReadingSpeedSlider_OnValueChanged(this, new RoutedPropertyChangedEventArgs<double>(ReadingSpeedSlider.Value, ReadingSpeedSlider.Value));
    }

    private void SaveVoiceOverrideForSelectedLanguage()
    {
        var languageCode = _voiceLanguageKey ?? _settings.SelectedLanguageCode.ToLowerInvariant();
        if (!_editedVoiceLanguages.Contains(languageCode)) return;
        var selectedVoiceId = SystemVoiceComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedVoiceId))
        {
            _settings.SystemVoiceIdOverrideByLanguage.Remove(languageCode);
            return;
        }

        _settings.SystemVoiceIdOverrideByLanguage[languageCode] = selectedVoiceId;
    }

    private void OnVoiceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_loading && !_refreshingVoices && _voiceLanguageKey is not null) _editedVoiceLanguages.Add(_voiceLanguageKey);
    }

    private static void OpenWindowsSettings(string uri)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
    }


    private T Find<T>(string name) where T : FrameworkElement => (T)(_host.FindName(name) ?? throw new InvalidOperationException("Missing settings control: " + name));
    private ComboBox LanguageComboBox => Find<ComboBox>("LanguageComboBox");
    private ComboBox PlacementModeComboBox => Find<ComboBox>("PlacementModeComboBox");
    private ComboBox SpeechLanguageModeComboBox => Find<ComboBox>("SpeechLanguageModeComboBox");
    private CheckBox GlobalSuggestionsCheckBox => Find<CheckBox>("GlobalSuggestionsCheckBox");
    private CheckBox DomainListsCheckBox => Find<CheckBox>("DomainListsCheckBox");
    private CheckBox UseSystemSpeechSettingsCheckBox => Find<CheckBox>("UseSystemSpeechSettingsCheckBox");
    private Slider ReadingSpeedSlider => Find<Slider>("ReadingSpeedSlider");
    private ComboBox ReadingStrategyComboBox => Find<ComboBox>("ReadingStrategyComboBox");
    private ComboBox ReadingHighlightModeComboBox => Find<ComboBox>("ReadingHighlightModeComboBox");
    private CheckBox TextAnalyzerEnabledCheckBox => Find<CheckBox>("TextAnalyzerEnabledCheckBox");
    private CheckBox TextAnalyzerColoringCheckBox => Find<CheckBox>("TextAnalyzerColoringCheckBox");
    private CheckBox SemanticDiagnosticsCheckBox => Find<CheckBox>("SemanticDiagnosticsCheckBox");
    private CheckBox PunctuationDiagnosticsCheckBox => Find<CheckBox>("PunctuationDiagnosticsCheckBox");
    private CheckBox ErrorTrackingEnabledCheckBox => Find<CheckBox>("ErrorTrackingEnabledCheckBox");
    private CheckBox StoreSentenceExamplesCheckBox => Find<CheckBox>("StoreSentenceExamplesCheckBox");
    private CheckBox PerformanceInstrumentationCheckBox => Find<CheckBox>("PerformanceInstrumentationCheckBox");
    private CheckBox AppDebugLoggingCheckBox => Find<CheckBox>("AppDebugLoggingCheckBox");
    private CheckBox PlacementDebugLoggingCheckBox => Find<CheckBox>("PlacementDebugLoggingCheckBox");
    private CheckBox CoreDebugLoggingCheckBox => Find<CheckBox>("CoreDebugLoggingCheckBox");
    private ComboBox SystemVoiceComboBox => Find<ComboBox>("SystemVoiceComboBox");
    private TextBlock VoiceStatusTextBlock => Find<TextBlock>("VoiceStatusTextBlock");
    private TextBlock ReadingSpeedValueText => Find<TextBlock>("ReadingSpeedValueText");
    private TextBlock VersionText => Find<TextBlock>("VersionText");
    private TextBlock UpdateStatusText => Find<TextBlock>("UpdateStatusText");
    private Button CheckUpdatesButton => Find<Button>("CheckUpdatesButton");
    private Button OpenUpdateButton => Find<Button>("OpenUpdateButton");
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _updateCancellation.Cancel(); _updates.Dispose(); _updateCancellation.Dispose();
        SystemVoiceComboBox.SelectionChanged -= OnVoiceChanged;
        LanguageComboBox.SelectionChanged -= LanguageComboBox_OnSelectionChanged;
        UseSystemSpeechSettingsCheckBox.Checked -= SpeechSettings_OnChanged;
        UseSystemSpeechSettingsCheckBox.Unchecked -= SpeechSettings_OnChanged;
        ReadingSpeedSlider.ValueChanged -= ReadingSpeedSlider_OnValueChanged;
    }

    private sealed record SettingsOption(string Value, string Label);
}
