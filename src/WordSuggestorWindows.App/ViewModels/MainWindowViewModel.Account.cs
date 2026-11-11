using WordSuggestorWindows.App.Services;

namespace WordSuggestorWindows.App.ViewModels;

public sealed partial class MainWindowViewModel
{
    private AccountSessionCoordinator? _accountSession;
    public AccountSessionCoordinator AccountSession => _accountSession ??= AccountSessionCoordinator.CreateDefault();
    private void DisposeAccountSession() { _accountSession?.Dispose(); _accountSession = null; }
}
