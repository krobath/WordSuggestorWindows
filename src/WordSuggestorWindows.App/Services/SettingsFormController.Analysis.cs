using System.IO;
using System.Windows;
using System.Windows.Controls;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

internal sealed partial class SettingsFormController
{
    private long _analysisCapabilityRevision;
    private void AttachAnalysisControls()
    {
        foreach (var name in new[] { "ContextualCommaCheckBox", "SaveWordChoicesCheckBox", "ShareWordChoicesCheckBox" })
        {
            if (Optional<CheckBox>(name) is not { } check) continue;
            RoutedEventHandler handler = (_, _) => CommitLive(); check.Checked += handler; check.Unchecked += handler;
            _unsubscribe.Add(() => { check.Checked -= handler; check.Unchecked -= handler; });
        }
        if (Optional<ComboBox>("CommaStyleComboBox") is { } combo)
        {
            SelectionChangedEventHandler handler = (_, _) => CommitLive(); combo.SelectionChanged += handler;
            _unsubscribe.Add(() => combo.SelectionChanged -= handler);
        }
        _viewModel.SavedChoicesChanged += StoredChoicesChanged;
        _unsubscribe.Add(() => _viewModel.SavedChoicesChanged -= StoredChoicesChanged);
    }
    private void StoredChoicesChanged(object? sender, EventArgs e) => RefreshSavedChoices();
    private void LoadAnalysisControls()
    {
        Examples.IsEnabled = _settings.IsErrorTrackingEnabled;
        if (Optional<ComboBox>("CommaStyleComboBox") is { } comma)
        {
            comma.ItemsSource = new[] { Option("grammatical_without_start_comma", "CommaWithout"), Option("grammatical_with_start_comma", "CommaWith") };
            comma.SelectedValue = _settings.PunctuationCommaStyle;
            Find<CheckBox>("ContextualCommaCheckBox").IsChecked = _settings.ContextualCommaMode == "boundary_local_contextual_v1";
            Find<CheckBox>("SaveWordChoicesCheckBox").IsChecked = _settings.SaveWordChoices;
            Find<CheckBox>("ShareWordChoicesCheckBox").IsChecked = _settings.ShareWordChoices;
            Find<CheckBox>("ShareWordChoicesCheckBox").IsEnabled = _settings.SaveWordChoices;
            RefreshSavedChoices();
        }
        _ = RefreshAnalysisCapabilitiesAsync();
    }
    private void ReadAnalysisControls()
    {
        if (Optional<ComboBox>("CommaStyleComboBox") is not { } comma) return;
        _settings.PunctuationCommaStyle = comma.SelectedValue as string ?? _settings.PunctuationCommaStyle;
        _settings.ContextualCommaMode = Find<CheckBox>("ContextualCommaCheckBox").IsChecked == true ? "boundary_local_contextual_v1" : "legacy_graph_only";
        _settings.SaveWordChoices = Find<CheckBox>("SaveWordChoicesCheckBox").IsChecked == true;
        _settings.ShareWordChoices = _settings.SaveWordChoices && Find<CheckBox>("ShareWordChoicesCheckBox").IsChecked == true;
    }
    private async Task RefreshAnalysisCapabilitiesAsync()
    {
        var revision = ++_analysisCapabilityRevision;
        var capabilities = CoreCapabilities.Legacy;
        try { if (_viewModel.CoreSettings is { } adapter) capabilities = await adapter.GetCapabilitiesAsync(cancellationToken: _updateCancellation.Token); }
        catch (Exception ex) when (ex is InvalidOperationException or OperationCanceledException or NotSupportedException or IOException or TimeoutException) { }
        if (_disposed || revision != _analysisCapabilityRevision) return;
        Analysis.IsEnabled = capabilities.DocumentReadings;
        Colouring.IsEnabled = capabilities.DocumentReadings && _settings.IsTextAnalyzerEnabled;
        Semantic.IsEnabled = Punctuation.IsEnabled = capabilities.ContextDiagnostics && capabilities.DocumentReadings && _settings.IsTextAnalyzerEnabled;
        if (Optional<ComboBox>("CommaStyleComboBox") is { } comma)
        {
            comma.IsEnabled = Punctuation.IsEnabled && _settings.IsPunctuationDiagnosticsEnabled;
            Find<CheckBox>("ContextualCommaCheckBox").IsEnabled = comma.IsEnabled;
            Find<TextBlock>("AnalysisCapabilityStatus").Text = SettingsText.Get(capabilities.DocumentReadings ? "AnalysisNote" : "AnalysisUnavailable");
        }
    }
    private void RefreshSavedChoices()
    {
        if (Optional<Button>("SavedChoicesButton") is not { } button) return;
        try
        {
            var choices = _viewModel.LoadSavedChoices(); button.Content = SettingsText.Format("SavedChoiceCount", choices.Count);
            Find<ItemsControl>("SavedChoicesList").ItemsSource = choices;
            Find<TextBlock>("SavedChoicesStatus").Text = choices.Count == 0 ? SettingsText.Get("ChoicesEmpty") : SettingsText.Get("LocalOnly");
            Find<Button>("DeleteAllChoicesButton").IsEnabled = choices.Count > 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.Text.Json.JsonException)
        { button.Content = SettingsText.Get("Unavailable"); Find<TextBlock>("SavedChoicesStatus").Text = SettingsText.Get("LearningUnavailable"); Find<ItemsControl>("SavedChoicesList").ItemsSource = null; }
    }
    public void CloseSavedChoices()
    {
        if (Optional<StackPanel>("SavedChoicesPanel") is { } panel) panel.Visibility = Visibility.Collapsed;
        if (Optional<GroupBox>("LearningPreferencesPanel") is { } preferences) preferences.Visibility = Visibility.Visible;
    }
    public void SavedChoices_OnClick(object sender, RoutedEventArgs e)
    { RefreshSavedChoices(); Find<GroupBox>("LearningPreferencesPanel").Visibility = Visibility.Collapsed; Find<StackPanel>("SavedChoicesPanel").Visibility = Visibility.Visible; }
    public void SavedChoicesBack_OnClick(object sender, RoutedEventArgs e) => CloseSavedChoices();
    public void DeleteChoice_OnClick(object sender, RoutedEventArgs e)
    { if (sender is Button { DataContext: SavedWordChoice choice }) ShowChoiceResult(_viewModel.DeleteSavedChoice(choice.Id)); }
    public void DeleteAllChoices_OnClick(object sender, RoutedEventArgs e)
    { if (MessageBox.Show(_host, SettingsText.Get("DeleteAllConfirm"), "WordSuggestor", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) ShowChoiceResult(_viewModel.DeleteAllSavedChoices()); }
    public void ResetLearning_OnClick(object sender, RoutedEventArgs e)
    { if (MessageBox.Show(_host, SettingsText.Get("ResetLearningConfirm"), "WordSuggestor", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) ShowChoiceResult(_viewModel.ResetLocalLearning()); }
    private void ShowChoiceResult(bool succeeded)
    {
        if (succeeded) { RefreshSavedChoices(); return; }
        Find<TextBlock>("SettingsSaveStatus").Text = _viewModel.StatusMessage; _statusKey = null;
    }
}
