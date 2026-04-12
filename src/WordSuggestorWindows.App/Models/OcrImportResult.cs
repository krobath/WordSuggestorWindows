namespace WordSuggestorWindows.App.Models;

public sealed record OcrImportResult(
    string Text,
    string Source,
    int LineCount,
    DateTimeOffset CapturedAt);
