using WordSuggestorWindows.App.Models;

namespace WordSuggestorWindows.App.Services;

// Engine boundary for later Settings slices. No UI feature is enabled merely
// because this transport exists; callers must check capabilities and scope.
public interface ICoreSettingsAdapter
{
    Task<CoreCapabilities> GetCapabilitiesAsync(string? catalogPath = null, CancellationToken cancellationToken = default);
    Task<CoreQueryResult<IReadOnlyList<CoreCandidate>>> SuggestWithOptionsAsync(string text, CoreRequestContext context,
        CoreSuggestionOptions options, CancellationToken cancellationToken = default);
    Task<CoreQueryResult<IReadOnlyList<CoreDomain>>> GetDomainsAsync(string? catalogPath, CoreRequestContext context, CancellationToken cancellationToken = default);
    Task<CoreQueryResult<IReadOnlyList<CoreCandidate>>> SuggestDomainAsync(string text, string catalogPath, string domainId,
        CoreRequestContext context, CancellationToken cancellationToken = default);
    Task<CoreQueryResult<CoreWordInfo>> GetWordInfoAsync(string surface, CoreRequestContext context, bool includeFamilies = false, CancellationToken cancellationToken = default);
    Task<CoreQueryResult<IReadOnlyList<CoreDiagnostic>>> AnalyzeAsync(string text, CoreRequestContext context,
        CoreAnalysisOptions options, CancellationToken cancellationToken = default);
}
