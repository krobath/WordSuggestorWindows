namespace WordSuggestorWindows.App.Models;

public sealed record SelectionImportResult(
    string Text,
    string Source,
    DateTimeOffset CapturedAt,
    IntPtr WindowHandle);
