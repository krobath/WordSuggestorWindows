using System.Text.Json.Serialization;

namespace WordSuggestorWindows.App.Models;

public enum DiagnosticCategory { App, Placement, Panel, Core, Performance }
public enum DiagnosticEvent { SettingsApplied, LocalSession, ExternalSession, CommitAccepted, CommitRejected, SuggestionsReady, SuggestionsUnavailable, OverlayHidden, OverlayLocal, OverlayExternal, CaretConfirmed, StaticPlacement, CaretFallback, NativeEditVerified, NativeEditRejected, CoreUnavailable, CorePackUnavailable, CoreCompleted, CoreFailed, CoreCancelled, CoreTimedOut, CoreRejected, CoreSummary, Aggregate }
public enum DiagnosticMetric { None, Suggestions, CoreRoundtrip, OverlayPlacement, NativeCommit }
public enum DiagnosticOperation { None, Legacy, Capabilities, Suggest, Domains, DomainSuggestions, WordInfo, Analyze, Document, SmartFollowup, LivePeriod }
public enum DiagnosticHealth { Disabled, Ready, WriteUnavailable }

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CoreOperationalDiagnostics([property: JsonRequired] int SchemaVersion,
    [property: JsonRequired] string Operation, [property: JsonRequired] string Status,
    [property: JsonRequired] long ElapsedMilliseconds, [property: JsonRequired] int CandidateCount,
    [property: JsonRequired] int DiagnosticCount, [property: JsonRequired] int DocumentWordCount)
{
    public bool ValidFor(string operation, bool rejected) => SchemaVersion == 1 && Operation == operation &&
        Status == (rejected ? "rejected" : "completed") && ElapsedMilliseconds is >= 0 and <= 60000 &&
        CandidateCount is >= 0 and <= 40 && DiagnosticCount is >= 0 and <= 40 && DocumentWordCount is >= 0 and <= 512;
}

// No free-form message, path, request/document/window ID, word or text field.
public sealed record RuntimeDiagnostic(int SchemaVersion, DateTimeOffset Timestamp, DiagnosticCategory Category, DiagnosticEvent Event,
    DiagnosticOperation Operation = DiagnosticOperation.None, DiagnosticMetric Metric = DiagnosticMetric.None,
    long Count = 0, long TotalMicroseconds = 0, long MaxMicroseconds = 0, int CandidateCount = 0, int DiagnosticCount = 0, int DocumentWordCount = 0);
