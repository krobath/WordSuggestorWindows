using System.Windows;
using System.Windows.Controls;
using WordSuggestorWindows.App.Models;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class SettingsWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly AppSettingsSnapshot _settings;

    public SettingsWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        _settings = viewModel.LoadSettingsSnapshot().Clone();
        InitializeComponent();
        InitializeOptionSources();
        LoadControlsFromSettings();
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

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
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

        _viewModel.ApplySettingsSnapshot(_settings);
        Close();
    }

    private void CancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
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

    private void OpenSpeechSettingsButton_OnClick(object sender, RoutedEventArgs e) =>
        OpenWindowsSettings("ms-settings:speech");

    private void OpenLanguageSettingsButton_OnClick(object sender, RoutedEventArgs e) =>
        OpenWindowsSettings("ms-settings:regionlanguage");

    private void RefreshSpeechControls()
    {
        if (SystemVoiceComboBox is null)
        {
            return;
        }

        var languageCode = LanguageComboBox.SelectedValue as string ?? _settings.SelectedLanguageCode;
        var languageKey = languageCode.ToLowerInvariant();
        var matchingVoices = WindowsVoiceCatalogService.GetVoiceOptionsForLanguage(languageCode);
        var oneCoreStatus = WindowsTextToSpeechService.GetOneCoreRuntimeStatus();
        var options = new List<SettingsOption>
        {
            new(string.Empty, "Automatisk (bedst)")
        };
        options.AddRange(matchingVoices.Select(voice => new SettingsOption(voice.Id, voice.DisplayLabel)));

        SystemVoiceComboBox.ItemsSource = options;
        SystemVoiceComboBox.SelectedValue =
            _settings.SystemVoiceIdOverrideByLanguage.TryGetValue(languageKey, out var voiceId) &&
            options.Any(option => string.Equals(option.Value, voiceId, StringComparison.OrdinalIgnoreCase))
                ? voiceId
                : string.Empty;

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
        var languageCode = _settings.SelectedLanguageCode.ToLowerInvariant();
        var selectedVoiceId = SystemVoiceComboBox.SelectedValue as string;
        if (string.IsNullOrWhiteSpace(selectedVoiceId))
        {
            _settings.SystemVoiceIdOverrideByLanguage.Remove(languageCode);
            return;
        }

        _settings.SystemVoiceIdOverrideByLanguage[languageCode] = selectedVoiceId;
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

    private sealed record SettingsOption(string Value, string Label);
}
