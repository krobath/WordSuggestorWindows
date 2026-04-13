using System.Windows;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App;

public partial class InsightsWindow : Window
{
    public InsightsWindow(ErrorInsightsSnapshot snapshot)
    {
        InitializeComponent();
        DataContext = snapshot;
    }
}
