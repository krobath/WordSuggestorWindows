using System.Text.Json.Serialization;

namespace WordSuggestorWindows.App.Models;

public sealed record CoreRequestContext([property: JsonRequired] Guid DocumentId, [property: JsonRequired] long Generation);
public sealed record CoreQueryResult<T>(CoreRequestContext Context, string Language, T Value, string? WarningCode)
{
    public bool IsCurrent(CoreRequestContext context, string language) => Context == context && Language == language;
}

public sealed record CoreCapabilities
{
    public int SchemaVersion { get; init; }
    public int[] ProtocolVersions { get; init; } = [];
    public string[] Operations { get; init; } = [];
    public bool SourcePreferences { get; init; }
    public bool CommitPlans { get; init; }
    public bool LivePeriodAssist { get; init; }
    public bool Phonetic { get; init; }
    public bool Misspellings { get; init; }
    public bool Synonyms { get; init; }
    public bool ContextReranking { get; init; }
    public bool WordFamilies { get; init; }
    public bool MorphologicalAnalyses { get; init; }
    public bool ContextDiagnostics { get; init; }
    public bool DomainCatalog { get; init; }
    public bool InterpretationChoices { get; init; }
    public bool ChoicePersistence { get; init; }
    public bool AdaptiveLearning { get; init; }
    public bool Supports(string operation) => ProtocolVersions.Contains(2) && Operations.Contains(operation, StringComparer.Ordinal);
    public static CoreCapabilities Legacy { get; } = new() { SchemaVersion = 1, ProtocolVersions = [1], Operations = ["suggest", "addUserWord"] };
}

public sealed record CoreSuggestionOptions(bool? ConfusionSet = null, bool? Phonetic = null, bool? Misspelling = null,
    bool? Synonym = null, bool? ContextBased = null, string? RuntimeBudget = null, string? SpacingMode = null, string? Suffix = null);
public sealed record CoreAnalysisOptions(bool Semantic = false, bool Punctuation = false,
    string CommaStyle = "grammatical_without_start_comma", string CommaMode = "boundary_local_contextual_v1");
public sealed record CoreCandidate([property: JsonRequired] string Term, [property: JsonRequired] string InsertText,
    [property: JsonRequired] double Score, [property: JsonRequired] string Kind, [property: JsonRequired] string Type,
    string? Pos, string? Gram, CoreCommitPlan? CommitPlan = null);
public sealed record CoreCommitPlan([property: JsonRequired] string Text, [property: JsonRequired] bool AutoSpace);
public sealed record CoreFollowup(string? Replacement);
public sealed record CorePeriodDecision(CoreDiagnosticEdit? Edit);
public sealed record CoreDomain([property: JsonRequired] string Id, [property: JsonRequired] string Title,
    [property: JsonRequired] string Version, [property: JsonRequired] string Status, [property: JsonRequired] int RowCount);
public sealed record CoreSurfaceAnalysis([property: JsonRequired] string Id, [property: JsonRequired] string Gram,
    [property: JsonRequired] string Pos, [property: JsonRequired] string WordClass, string? Lemma);
public sealed record CoreFamilyForm([property: JsonRequired] string Term, [property: JsonRequired] string Gram, string? Lemma, CoreCommitPlan? CommitPlan = null);
public sealed record CoreWordInfo(IReadOnlyList<CoreSurfaceAnalysis> Analyses, IReadOnlyList<CoreFamilyForm> Families);
public sealed record CoreDiagnosticEdit([property: JsonRequired] int Start, [property: JsonRequired] int Length,
    [property: JsonRequired] string Replacement);
public sealed record CoreDiagnostic([property: JsonRequired] int Start, [property: JsonRequired] int Length,
    [property: JsonRequired] string IssueType, [property: JsonRequired] string Severity, string? RuleId,
    [property: JsonRequired] string Message, [property: JsonRequired] double Confidence, string? Requirement,
    string? GroupId, [property: JsonRequired] CoreDiagnosticEdit[] Edits, [property: JsonRequired] string[] Replacements);
