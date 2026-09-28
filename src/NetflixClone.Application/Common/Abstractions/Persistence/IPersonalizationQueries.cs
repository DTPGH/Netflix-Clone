using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Personalization;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IPersonalizationQueries
{
    Task<bool> AreMoviesEligibleAsync(IReadOnlyList<int> ids, byte maturityLevel, CancellationToken ct = default);
    Task<BrowseMoviesResult> GetOnboardingMoviesAsync(byte maturityLevel, int page, int pageSize, string? search, CancellationToken ct = default);
    Task<RecommendationsResult> GetRecommendationsAsync(int profileId, byte maturityLevel, int limit, CancellationToken ct = default);
}
