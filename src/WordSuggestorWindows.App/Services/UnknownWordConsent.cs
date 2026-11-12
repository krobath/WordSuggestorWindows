using System.Windows;

namespace WordSuggestorWindows.App.Services;

public static class UnknownWordConsent
{
    public static bool Request(Window owner) => MessageBox.Show(owner, SettingsText.Get("UnknownConsent"),
        "WordSuggestor", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
}
