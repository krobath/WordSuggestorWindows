using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public sealed class UserWordsWindow : Window
{
    public UserWordsWindow(MainWindowViewModel viewModel)
    {
        Title = "WordSuggestor – Brugerord";
        Width = 440; Height = 260; ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = "Tilføj et brugerord", FontSize = 20, Margin = new Thickness(0, 0, 0, 12) });
        var input = new TextBox { MaxLength = 128, Margin = new Thickness(0, 0, 0, 12) };
        AutomationProperties.SetName(input, "Brugerord");
        panel.Children.Add(input);
        var status = new TextBlock { Text = "Ordet gemmes lokalt for det valgte sprog.", TextWrapping = TextWrapping.Wrap };
        var add = new Button { Content = "Tilføj ord", IsDefault = true, Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 0, 0, 12) };
        add.Click += async (_, _) =>
        {
            add.IsEnabled = false;
            try { await viewModel.AddUserWordAsync(input.Text); input.Clear(); status.Text = "Ordet er gemt og klar til ordforslag."; }
            catch (Exception) { status.Text = "Ordet kunne ikke gemmes. Skriv ét ord, og kontrollér sprogpakken."; }
            finally { add.IsEnabled = true; }
        };
        panel.Children.Add(add); panel.Children.Add(status);
        Content = panel;
        Loaded += (_, _) => input.Focus();
    }
}
