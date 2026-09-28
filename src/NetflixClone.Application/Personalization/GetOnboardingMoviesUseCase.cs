using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Personalization;
public interface IGetOnboardingMoviesUseCase
{
    Task<Result<BrowseMoviesResult>> ExecuteAsync(OnboardingMoviesQuery query, CancellationToken ct = default);
}
public sealed class GetOnboardingMoviesUseCase(IProfileRepository profiles, IPersonalizationQueries movies) : IGetOnboardingMoviesUseCase
{
    public async Task<Result<BrowseMoviesResult>> ExecuteAsync(OnboardingMoviesQuery query, CancellationToken ct = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(query.UserAccountId, query.ProfileId, ct);
        if (profile is null || profile.IsDeleted) return Result<BrowseMoviesResult>.Failure(PersonalizationErrors.ProfileNotFound);
        var search = query.Search?.Trim();
        if (query.Page < 1 || query.PageSize is < 1 or > 50 || (long)(query.Page - 1) * query.PageSize > int.MaxValue || search?.Length > 100)
            return Result<BrowseMoviesResult>.Failure(PersonalizationErrors.InvalidQuery);
        return Result<BrowseMoviesResult>.Success(await movies.GetOnboardingMoviesAsync(profile.MaturityLevel, query.Page, query.PageSize, search, ct));
    }
}
