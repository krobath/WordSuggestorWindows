namespace WordSuggestorWindows.App.Models;

public sealed class ExternalSuggestionTokenChangedEventArgs : EventArgs
{
    public ExternalSuggestionTokenChangedEventArgs(
        IntPtr windowHandle,
        string token,
        bool isBoundary,
        DateTimeOffset capturedAt)
    {
        WindowHandle = windowHandle;
        Token = token;
        IsBoundary = isBoundary;
        CapturedAt = capturedAt;
    }

    public IntPtr WindowHandle { get; }

    public string Token { get; }

    public bool IsBoundary { get; }

    public DateTimeOffset CapturedAt { get; }
}
