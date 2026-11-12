using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

public sealed class CoreBridgeRejectedException(string code) : InvalidOperationException("Core afviste den udvidede forespørgsel.")
{
    public string Code { get; } = code.All(c => c is >= 'a' and <= 'z' or '_') && code.Length <= 64 ? code : "rejected";
}

public sealed partial class WordSuggestorCoreCliSuggestionProvider
{
    private static readonly JsonSerializerOptions ExtendedJson = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    private readonly object _capabilityGate = new();
    private readonly Dictionary<string, CoreCapabilities> _capabilities = [];

    public async Task<CoreCapabilities> GetCapabilitiesAsync(string? catalogPath = null, CancellationToken cancellationToken = default)
    {
        var language = RequireLanguagePack();
        return await ReadCapabilitiesAsync(language, catalogPath, cancellationToken);
    }

    private async Task<CoreCapabilities> ReadCapabilitiesAsync(LanguageOption language, string? catalogPath, CancellationToken cancellationToken)
    {
        static string Stamp(string? path) => path is not null && File.Exists(path) ? path + ":" + new FileInfo(path).Length + ":" + File.GetLastWriteTimeUtc(path).Ticks : path ?? "";
        var key = language.LanguageCode + "|" + Stamp(language.PackPath) + "|" + Stamp(_cliPath) + "|" + Stamp(catalogPath);
        lock (_capabilityGate) if (_capabilities.TryGetValue(key, out var cached)) return cached;
        var id = Guid.NewGuid().ToString("N");
        var request = new { version = 1, requestId = id, operation = "capabilities", language = language.LanguageCode,
            packPath = language.PackPath, textBeforeCaret = "", maxSuggestions = 40, catalogPath };
        var response = Decode<CapabilityEnvelope>(await ExchangeAsync(request, "--stdio-json-v1", cancellationToken, DiagnosticOperation.Capabilities));
        if (response.Version != 1 || response.RequestId != id) throw InvalidProtocol();
        // Explicit old-protocol rejection is the only downgrade signal. Network,
        // timeout, corruption or process errors never masquerade as capabilities.
        if (response.ErrorCode == "invalid_request") return CoreCapabilities.Legacy;
        if (response.ErrorCode is { } error) throw new CoreBridgeRejectedException(error);
        var caps = response.Capabilities;
        if (caps is null || caps.SchemaVersion != 1 || caps.ProtocolVersions is null ||
            !caps.ProtocolVersions.Contains(2) || caps.ProtocolVersions.Length > 4 || caps.Operations is null ||
            caps.Operations.Length > 16 || caps.Operations.Any(s => !Safe(s, 64))) throw InvalidProtocol();
        lock (_capabilityGate) { if (_capabilities.Count >= 16) _capabilities.Clear(); _capabilities[key] = caps; }
        return caps;
    }

    public async Task<CoreQueryResult<IReadOnlyList<CoreCandidate>>> SuggestWithOptionsAsync(string text, CoreRequestContext context,
        CoreSuggestionOptions options, CancellationToken cancellationToken = default)
    {
        var response = await QueryAsync("suggest", text, context, options, null, null, cancellationToken);
        HealthWarning = response.WarningCode == "user_words_unavailable" ? "Brugerord kunne ikke indlæses. Sprogpakken bruges fortsat, og brugerdata er bevaret." : null;
        return Result(response, ValidateCandidates(response.Suggestions));
    }

    public async Task<CoreQueryResult<IReadOnlyList<CoreDomain>>> GetDomainsAsync(string? catalogPath, CoreRequestContext context, CancellationToken cancellationToken = default)
    {
        var response = await QueryAsync("domains", "", context, null, catalogPath, null, cancellationToken);
        var rows = response.Domains;
        if (rows is null || rows.Length > 40 || rows.Any(d => !Safe(d.Id, 128) || !Safe(d.Title, 512) || !Safe(d.Version, 128) || !Safe(d.Status, 128) || d.RowCount < 0)) throw InvalidProtocol();
        return Result<IReadOnlyList<CoreDomain>>(response, rows);
    }

    public async Task<CoreQueryResult<IReadOnlyList<CoreCandidate>>> SuggestDomainAsync(string text, string catalogPath, string domainId,
        CoreRequestContext context, CancellationToken cancellationToken = default, CoreSuggestionOptions? options = null)
    {
        if (!Safe(domainId, 128)) throw new ArgumentException("Invalid domain identifier.");
        var response = await QueryAsync("domainSuggestions", text, context, options is null ? null : new { options.RuntimeBudget, options.SpacingMode, options.Suffix }, catalogPath, domainId, cancellationToken);
        return Result(response, ValidateCandidates(response.Suggestions));
    }

    public async Task<CoreQueryResult<CoreWordInfo>> GetWordInfoAsync(string surface, CoreRequestContext context, bool includeFamilies = false, CancellationToken cancellationToken = default, string? spacingMode = null, string? suffix = null)
    {
        if (!Safe(surface, 128)) throw new ArgumentException("Invalid word-info input.");
        var response = await QueryAsync("wordInfo", surface, context, new { includeFamilies, spacingMode, suffix }, null, null, cancellationToken);
        if (response.Analyses is null || response.Families is null || response.Analyses.Length > 40 || response.Families.Length > 40 ||
            response.Analyses.Any(a => !Safe(a.Id, 4096) || !Safe(a.Gram, 512) || !Safe(a.Pos, 64) || !Safe(a.WordClass, 64)) ||
            response.Families.Any(f => !Safe(f.Term, 512) || f.Gram is null || f.Gram.Length > 512 || !ValidPlan(f.Term, f.CommitPlan))) throw InvalidProtocol();
        return Result(response, new CoreWordInfo(response.Analyses, response.Families));
    }

    public async Task<CoreQueryResult<CoreFollowup>> GetSmartFollowupAsync(string input, CoreRequestContext context, CancellationToken cancellationToken = default)
    {
        var response = await QueryAsync("smartFollowup", input, context, null, null, null, cancellationToken);
        if (response.Followup is not { } result || result.Replacement is { } replacement && replacement != input && replacement != " ") throw InvalidProtocol();
        return Result(response, result);
    }
    public async Task<CoreQueryResult<CorePeriodDecision>> GetLivePeriodAsync(string prefix, CoreRequestContext context, CancellationToken cancellationToken = default)
    {
        var response = await QueryAsync("livePeriod", prefix, context, null, null, null, cancellationToken);
        if (response.PeriodDecision is not { } result || result.Edit is { } edit && (!ValidRange(prefix, edit.Start, edit.Length) || edit.Replacement != "." || edit.Length != 0)) throw InvalidProtocol();
        return Result(response, result);
    }

    public async Task<CoreQueryResult<IReadOnlyList<CoreDiagnostic>>> AnalyzeAsync(string text, CoreRequestContext context,
        CoreAnalysisOptions options, CancellationToken cancellationToken = default)
    {
        var response = await QueryAsync("analyze", text, context, options, null, null, cancellationToken);
        ValidateDiagnostics(text, response.Diagnostics);
        return Result<IReadOnlyList<CoreDiagnostic>>(response, response.Diagnostics!);
    }

    public async Task<CoreQueryResult<CoreDocumentResult>> AnalyzeDocumentAsync(string text, CoreRequestContext context, CoreAnalysisOptions options, CancellationToken cancellationToken = default)
    {
        var response = await QueryAsync("document", text, context, options, null, null, cancellationToken);
        ValidateDiagnostics(text, response.Diagnostics);
        if (response.Document is not { SchemaVersion: 1 } document || document.Words is null || document.Words.Length > 512) throw InvalidProtocol();
        var end = 0;
        foreach (var word in document.Words)
        {
            if (word.Start < end || word.Length <= 0 || !ValidRange(text, word.Start, word.Length) || !Safe(word.WordClass, 64) ||
                word.Readings is null || word.Readings.Length > 40 || word.Readings.Any(r => !Safe(r.Id, 4096) || !Safe(r.WordClass, 64) || r.Gram is null || r.Gram.Length > 512 || !Safe(r.Pos, 64))) throw InvalidProtocol();
            end = word.Start + word.Length;
        }
        return Result(response, new CoreDocumentResult(document, response.Diagnostics!));
    }

    private static void ValidateDiagnostics(string text, CoreDiagnostic[]? diagnostics)
    {
        if (diagnostics is null || diagnostics.Length > 40 || diagnostics.Any(d =>
            !ValidRange(text, d.Start, d.Length) || !Safe(d.Message, 4096) || !Safe(d.IssueType, 64) || !Safe(d.Severity, 64) ||
            !double.IsFinite(d.Confidence) || d.Confidence is < 0 or > 1 || d.Edits is null || d.Edits.Length > 128 ||
            d.Edits.Any(e => !ValidRange(text, e.Start, e.Length) || e.Replacement is null || e.Replacement.Length > 512) ||
            d.Replacements is null || d.Replacements.Length > 40 || d.Replacements.Any(s => s is null || s.Length > 512) ||
            (d.ExampleStart.HasValue != d.ExampleLength.HasValue) || d.ExampleStart is { } start && (!ValidRange(text, start, d.ExampleLength!.Value) || d.ExampleLength > 512))) throw InvalidProtocol();
    }

    private async Task<SettingsEnvelope> QueryAsync(string operation, string text, CoreRequestContext context, object? options,
        string? catalogPath, string? domainId, CancellationToken cancellationToken)
    {
        if (context.DocumentId == Guid.Empty || context.Generation < 0 || text.Length > 16000) throw new ArgumentException("Invalid Core request scope or size.");
        var language = RequireLanguagePack();
        var capabilities = await ReadCapabilitiesAsync(language, catalogPath, cancellationToken);
        if (!capabilities.Supports(operation)) throw new NotSupportedException("Denne Core-version understøtter ikke funktionen.");
        var id = Guid.NewGuid().ToString("N");
        var includeDiagnostics = Diagnostics?.Enabled(DiagnosticCategory.Core) == true && capabilities.OperationalDiagnostics;
        var request = new { version = 2, requestId = id, operation, language = language.LanguageCode,
            includeOperationalDiagnostics = includeDiagnostics ? true : (bool?)null,
            packPath = language.PackPath, text, maxResults = 40, context, options, catalogPath, domainId,
            userWordsPath = operation == "suggest" ? Path.Combine(_localDataRoot, "UserWords", "local_lexicon_overlay_v2.json") : null };
        // Produce a JsonElement with null option fields omitted. Unknown/nonapplicable
        // options are rejected by Core instead of being silently ignored.
        var payload = JsonSerializer.SerializeToElement(request, ExtendedJson);
        var response = Decode<SettingsEnvelope>(await ExchangeAsync(payload, "--stdio-json-v2", cancellationToken, WindowsDiagnostics.Operation(operation)));
        if (response.Version != 2 || response.RequestId != id || response.Context != context || response.Language != language.LanguageCode) throw InvalidProtocol();
        if (response.OperationalDiagnostics is { } trace && !trace.ValidFor(operation, response.ErrorCode is not null) || includeDiagnostics && response.OperationalDiagnostics is null)
        { Diagnostics?.Record(DiagnosticCategory.Core, DiagnosticEvent.CoreRejected, WindowsDiagnostics.Operation(operation)); throw InvalidProtocol(); }
        if (includeDiagnostics && response.OperationalDiagnostics is { } summary) Diagnostics?.RecordCore(summary);
        if (response.ErrorCode is { } error) throw new CoreBridgeRejectedException(error);
        return response;
    }

    private static IReadOnlyList<CoreCandidate> ValidateCandidates(CoreCandidate[]? rows)
    {
        if (rows is null || rows.Length > 40 || rows.Any(c => !Safe(c.Term, 512) || !Safe(c.InsertText, 512) || !Safe(c.Kind, 64) || !Safe(c.Type, 64) || !double.IsFinite(c.Score) || !ValidPlan(c.InsertText, c.CommitPlan))) throw InvalidProtocol();
        return rows;
    }
    private static bool ValidPlan(string insertion, CoreCommitPlan? plan) => plan is null || (plan.AutoSpace ? plan.Text == insertion + " " : plan.Text == insertion);
    private static bool Safe(string? value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum;
    private static bool ValidRange(string text, int start, int length) => start >= 0 && length >= 0 && start <= text.Length && length <= text.Length - start && Boundary(text, start) && Boundary(text, start + length);
    private static bool Boundary(string text, int index) => index == 0 || index == text.Length || !(char.IsLowSurrogate(text[index]) && char.IsHighSurrogate(text[index - 1]));
    private static InvalidOperationException InvalidProtocol() => new("Core returnerede en ugyldig settingskontrakt.");
    private static CoreQueryResult<T> Result<T>(SettingsEnvelope response, T value) => new(response.Context!, response.Language!, value, response.WarningCode);
    private sealed record CapabilityEnvelope(int Version, string RequestId, string? ErrorCode, CoreCapabilities? Capabilities);
    private sealed record SettingsEnvelope(int Version, string RequestId, CoreRequestContext? Context, string? Language,
        string? ErrorCode, string? WarningCode, CoreCandidate[]? Suggestions, CoreDomain[]? Domains,
        CoreSurfaceAnalysis[]? Analyses, CoreFamilyForm[]? Families, CoreDiagnostic[]? Diagnostics, CoreFollowup? Followup, CorePeriodDecision? PeriodDecision, CoreDocumentReadings? Document, CoreOperationalDiagnostics? OperationalDiagnostics);
}
