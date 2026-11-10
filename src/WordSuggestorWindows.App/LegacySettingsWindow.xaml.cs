using System.Windows;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class LegacySettingsWindow : Window
{
    private readonly SettingsFormController _form;

    public LegacySettingsWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        _form = new SettingsFormController(this, viewModel);
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e) => _form.SaveButton_OnClick(sender, e);
    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => _form.CancelButton_OnClick(sender, e);
    private void CheckUpdates_OnClick(object sender, RoutedEventArgs e) => _form.CheckUpdates_OnClick(sender, e);
    private void OpenUpdate_OnClick(object sender, RoutedEventArgs e) => _form.OpenUpdate_OnClick(sender, e);
    private void OpenSpeechSettingsButton_OnClick(object sender, RoutedEventArgs e) => _form.OpenSpeechSettingsButton_OnClick(sender, e);
    private void OpenLanguageSettingsButton_OnClick(object sender, RoutedEventArgs e) => _form.OpenLanguageSettingsButton_OnClick(sender, e);
    // XAML's initialization notifications precede the shared controller.
    private void SpeechSettings_OnChanged(object sender, RoutedEventArgs e) { }
    private void ReadingSpeedSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }
}
