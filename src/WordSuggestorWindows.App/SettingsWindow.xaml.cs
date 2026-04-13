using System.Windows;
using WordSuggestorWindows.App.Models;
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
    }

    private void LoadControlsFromSettings()
    {
        LanguageComboBox.SelectedValue = _settings.SelectedLanguageCode;
        PlacementModeComboBox.SelectedValue = _settings.SuggestionPlacementMode;
        GlobalSuggestionsCheckBox.IsChecked = _settings.IsGlobalCaptureEnabled;
        DomainListsCheckBox.IsChecked = _settings.IsDomainListsEnabled;
        UseSystemSpeechSettingsCheckBox.IsChecked = _settings.UseSystemSpeechSettings;
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
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e)
    {
        _settings.SelectedLanguageCode = LanguageComboBox.SelectedValue as string ?? _settings.SelectedLanguageCode;
        _settings.SuggestionPlacementMode = PlacementModeComboBox.SelectedValue as string ?? "followCaret";
        _settings.IsGlobalCaptureEnabled = GlobalSuggestionsCheckBox.IsChecked == true;
        _settings.IsDomainListsEnabled = DomainListsCheckBox.IsChecked == true;
        _settings.UseSystemSpeechSettings = UseSystemSpeechSettingsCheckBox.IsChecked == true;
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

    private sealed record SettingsOption(string Value, string Label);
}
