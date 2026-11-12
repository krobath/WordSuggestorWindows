using System.Security.Principal;
using System.Windows.Threading;

namespace WordSuggestorWindows.App.Services;

internal sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private EventWaitHandle? _open;
    private RegisteredWaitHandle? _registration;
    public bool IsPrimary { get; }

    public SingleInstance()
    {
        var identity = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        var packageName = WindowsUpdateService.ReadInstalledRelease()?.Name ?? "WordSuggestor.Windows.Beta";
        if (Environment.GetEnvironmentVariable("WORDSUGGESTOR_DATA_ROOT") is { Length: > 0 } root)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(System.IO.Path.GetFullPath(root).ToUpperInvariant());
            packageName += ".Profile." + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))[..24];
        }
        var name = @"Local\" + packageName + "." + identity;
        _mutex = new Mutex(true, name, out var primary);
        IsPrimary = primary;
        _open = new EventWaitHandle(false, EventResetMode.AutoReset, name + ".OpenEditor");
        if (!primary) _open.Set();
    }

    public void Listen(Dispatcher dispatcher, Action openEditor)
    {
        if (IsPrimary) _registration = ThreadPool.RegisterWaitForSingleObject(_open!,
            (_, _) => dispatcher.BeginInvoke(openEditor), null, Timeout.Infinite, false);
    }

    public void Dispose()
    {
        _registration?.Unregister(null);
        _open?.Dispose();
        if (IsPrimary) _mutex.ReleaseMutex();
        _mutex.Dispose();
    }
}
