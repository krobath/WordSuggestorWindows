using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WordSuggestorWindows.App.Controls;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class SettingsWindow : Window
{
    private readonly MainWindowViewModel _viewModel;
    private readonly SettingsFormController _form;
    private readonly SettingsHelpCoordinator _help;
    private LegacySettingsWindow? _legacy;
    private readonly Dictionary<string, StackPanel> _pages;

    public SettingsWindow(MainWindowViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        _pages = new()
        {
            ["profile"] = ProfilePage, ["general"] = GeneralPage, ["speech"] = SpeechPage,
            ["suggestions"] = SuggestionsPage, ["textAnalysis"] = AnalysisPage,
            ["learning"] = LearningPage, ["errorTracking"] = ErrorsPage, ["advanced"] = AdvancedPage
        };
        _help = new SettingsHelpCoordinator(this);
        AddHandler(SettingsRow.HelpRequestedEvent, new EventHandler<SettingsHelpEventArgs>((_, request) => _help.Toggle(request)));
        _form = new SettingsFormController(this, viewModel);
        CategoryNavigation.SelectedIndex = 0;
        var area = SystemParameters.WorkArea;
        Width = Math.Min(Width, area.Width); Height = Math.Min(Height, area.Height);
    }

    private void Category_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_pages is null || CategoryNavigation.SelectedItem is not ListBoxItem { Tag: string id } item) return;
        _help.Close(false);
        foreach (var page in _pages) page.Value.Visibility = page.Key == id ? Visibility.Visible : Visibility.Collapsed;
        CategoryTitle.Text = System.Windows.Automation.AutomationProperties.GetName(item);
        DetailScroll.ScrollToTop();
    }

    private void Settings_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (_help.IsOpen) { _help.Close(true); e.Handled = true; return; }
        if (Keyboard.FocusedElement is ComboBox { IsDropDownOpen: true }) return;
        Close(); e.Handled = true;
    }

    private void OpenLegacy_OnClick(object sender, RoutedEventArgs e)
    {
        _help.Close(false);
        if (_legacy is { IsVisible: true }) { _legacy.Activate(); return; }
        _legacy = new LegacySettingsWindow(_viewModel) { Owner = this };
        _legacy.Closed += (_, _) => _legacy = null;
        _legacy.Show();
    }

    private void SaveButton_OnClick(object sender, RoutedEventArgs e) => _form.SaveButton_OnClick(sender, e);
    private void CancelButton_OnClick(object sender, RoutedEventArgs e) => _form.CancelButton_OnClick(sender, e);
    private void CheckUpdates_OnClick(object sender, RoutedEventArgs e) => _form.CheckUpdates_OnClick(sender, e);
    private void OpenUpdate_OnClick(object sender, RoutedEventArgs e) => _form.OpenUpdate_OnClick(sender, e);
    private void OpenSpeechSettingsButton_OnClick(object sender, RoutedEventArgs e) => _form.OpenSpeechSettingsButton_OnClick(sender, e);
    private void OpenLanguageSettingsButton_OnClick(object sender, RoutedEventArgs e) => _form.OpenLanguageSettingsButton_OnClick(sender, e);
}
