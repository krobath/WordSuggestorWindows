namespace WordSuggestorWindows.App.Models;

public sealed record OcrScreenClipCallback(
    string CorrelationId,
    int Code,
    string? Reason,
    string? Token,
    string RawUri)
{
    public bool IsSuccess => Code == 200;
}
