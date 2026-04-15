using System.Windows;

namespace WordSuggestorWindows.App.Models;

public enum SuggestionAnchorQuality
{
    Confirmed,
    Approximate
}

public sealed record ExternalSuggestionAnchorSnapshot(
    Rect ScreenRect,
    string Source,
    DateTimeOffset CapturedAt,
    IntPtr WindowHandle,
    SuggestionAnchorQuality Quality);
