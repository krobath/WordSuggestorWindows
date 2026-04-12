namespace WordSuggestorWindows.App.Models;

public sealed class SelectionImportDiagnostic : EventArgs
{
    public SelectionImportDiagnostic(DateTimeOffset timestamp, string stage, string outcome, string detail)
    {
        Timestamp = timestamp;
        Stage = stage;
        Outcome = outcome;
        Detail = detail;
    }

    public DateTimeOffset Timestamp { get; }

    public string Stage { get; }

    public string Outcome { get; }

    public string Detail { get; }

    public override string ToString() =>
        $"{Timestamp:HH:mm:ss.fff} [{Stage}] {Outcome}: {Detail}";
}
