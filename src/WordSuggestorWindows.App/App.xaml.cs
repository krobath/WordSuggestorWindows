using System.IO;
using System.Windows;
using WordSuggestorWindows.App.Services;
using WordSuggestorWindows.App.ViewModels;

namespace WordSuggestorWindows.App;

public partial class App : Application
{
    private SingleInstance? _singleInstance;
    protected override async void OnStartup(StartupEventArgs e)
    {
        if (WindowsOcrCallbackBridge.TryPersistStartupCallback(e.Args))
        {
            Shutdown(0);
            return;
        }

        base.OnStartup(e);
        if (await WindowsUpdateQualification.TryRunAsync(e.Args) is { } updateCode) { Shutdown(updateCode); return; }

        try
        {
            ReleaseQualificationProbe.ConfigureRoot(e.Args);
            if (await ReleaseQualificationProbe.TryRunAsync(e.Args) is { } probeCode) { Shutdown(probeCode); return; }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException) { Shutdown(2); return; }

        _singleInstance = new SingleInstance();
        if (!_singleInstance.IsPrimary) { Shutdown(0); return; }

        var startupText = ResolveStartupText(e.Args);
        var settingsService = new WindowsAppSettingsService();
        var viewModel = new MainWindowViewModel(
            new WordSuggestorCoreCliSuggestionProvider(),
            new WindowsErrorInsightsStore(),
            settingsService,
            startupText);
        var window = new MainWindow(viewModel);

        MainWindow = window;
        window.Show();
        _singleInstance.Listen(Dispatcher, window.OpenEditor);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private static string? ResolveStartupText(IReadOnlyList<string> args)
    {
        var configured = Environment.GetEnvironmentVariable("WORDSUGGESTOR_WINDOWS_STARTUP_TEXT");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], "--sample-text", StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
