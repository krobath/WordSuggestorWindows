using System.Windows.Threading;
using WordSuggestorWindows.App.Models;
namespace WordSuggestorWindows.App.Services;

// Read supported focused text, never raw keys. IME, paste and caret moves are
// observed from actual control state. Unsupported/secure controls are excluded.
public sealed class WindowsGlobalSuggestionCaptureService : IDisposable
{
    private readonly WindowsNativeTextAdapter _adapter = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(150) };
    private ExternalTextSnapshot? _last;
    private DateTimeOffset _suppressUntil;
    public WindowsGlobalSuggestionCaptureService() => _timer.Tick += Poll;
    public event EventHandler<SelectionImportDiagnostic>? DiagnosticEmitted;
    public event EventHandler<ExternalSuggestionTokenChangedEventArgs>? TokenChanged;
    public bool IsRunning => _timer.IsEnabled;
    public ExternalTextSnapshot? CurrentSnapshot => _last;
    public void Start(IntPtr ignoredWindowHandle) => _timer.Start();
    public void Stop() { _timer.Stop(); _last = null; }
    public void SuppressSyntheticInput(TimeSpan duration) => _suppressUntil = DateTimeOffset.UtcNow.Add(duration);
    public void ClearTokenAfterCommittedSuggestion() => _last = null;
    private void Poll(object? sender, EventArgs e)
    {
        if (DateTimeOffset.UtcNow < _suppressUntil) return;
        var snapshot = _adapter.ReadFocused();
        if (snapshot == _last) return;
        _last = snapshot;
        DiagnosticEmitted?.Invoke(this, new SelectionImportDiagnostic(DateTimeOffset.Now,
            "ExternalCapture", snapshot is null ? "UnsupportedOrUnfocused" : "NativeEdit", "No text recorded."));
        TokenChanged?.Invoke(this, new ExternalSuggestionTokenChangedEventArgs(snapshot?.Window ?? IntPtr.Zero,
            snapshot?.Token ?? "", string.IsNullOrEmpty(snapshot?.Token), DateTimeOffset.Now));
    }
    public void Dispose() { Stop(); _timer.Tick -= Poll; }
}
