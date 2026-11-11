using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

// Engine boundary for later Settings slices. No UI feature is enabled merely
// because this transport exists; callers must check capabilities and scope.
public interface ICoreSettingsAdapter
{
    Task<CoreQueryResult<CoreDocumentResult>> AnalyzeDocumentAsync(string text, CoreRequestContext context, CoreAnalysisOptions options, CancellationToken cancellationToken = default);
    Task<CoreCapabilities> GetCapabilitiesAsync(string? catalogPath = null, CancellationToken cancellationToken = default);
    Task<CoreQueryResult<IReadOnlyList<CoreCandidate>>> SuggestWithOptionsAsync(string text, CoreRequestContext context,
        CoreSuggestionOptions options, CancellationToken cancellationToken = default);
    Task<CoreQueryResult<IReadOnlyList<CoreDomain>>> GetDomainsAsync(string? catalogPath, CoreRequestContext context, CancellationToken cancellationToken = default);
    Task<CoreQueryResult<IReadOnlyList<CoreCandidate>>> SuggestDomainAsync(string text, string catalogPath, string domainId,
        CoreRequestContext context, CancellationToken cancellationToken = default, CoreSuggestionOptions? options = null);
    Task<CoreQueryResult<CoreWordInfo>> GetWordInfoAsync(string surface, CoreRequestContext context, bool includeFamilies = false, CancellationToken cancellationToken = default, string? spacingMode = null, string? suffix = null);
    Task<CoreQueryResult<CoreFollowup>> GetSmartFollowupAsync(string input, CoreRequestContext context, CancellationToken cancellationToken = default);
    Task<CoreQueryResult<CorePeriodDecision>> GetLivePeriodAsync(string prefix, CoreRequestContext context, CancellationToken cancellationToken = default);
    Task<CoreQueryResult<IReadOnlyList<CoreDiagnostic>>> AnalyzeAsync(string text, CoreRequestContext context,
        CoreAnalysisOptions options, CancellationToken cancellationToken = default);
}
