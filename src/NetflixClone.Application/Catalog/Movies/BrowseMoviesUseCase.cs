using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Catalog.Movies;
public sealed class BrowseMoviesUseCase(IMovieCatalogQueries movies, IProfileRepository profiles) : IBrowseMoviesUseCase
{
    public async Task<Result<BrowseMoviesResult>> ExecuteAsync(BrowseMoviesQuery query, CancellationToken cancellationToken = default)
    {
        byte? maxAge = null;
        if (query.ProfileId.HasValue || query.UserAccountId.HasValue)
        {
            if (query.ProfileId is not > 0 || query.UserAccountId is not > 0)
                return Result<BrowseMoviesResult>.Failure(NetflixClone.Application.Profiles.ProfileErrors.NotFound);
            var profile = await profiles.GetByIdForAccountAsync(query.UserAccountId.Value, query.ProfileId.Value, cancellationToken);
            if (profile is null || profile.IsDeleted)
                return Result<BrowseMoviesResult>.Failure(NetflixClone.Application.Profiles.ProfileErrors.NotFound);
            maxAge = profile.MaturityLevel;
        }
        var search = query.Search?.Trim();
        if (query.Page < 1 || query.PageSize is < 1 or > 50 || search?.Length > 100 ||
            query.GenreId is <= 0 || (long)(query.Page - 1) * query.PageSize > int.MaxValue ||
            query.Sort is not ("releaseDateDesc" or "titleAsc"))
            return Result<BrowseMoviesResult>.Failure(MovieErrors.InvalidQuery);
        var criteria = new MovieCatalogCriteria(query.Page, query.PageSize,
            string.IsNullOrEmpty(search) ? null : search, query.GenreId,
            query.Sort == "titleAsc" ? MovieCatalogSort.TitleAscending : MovieCatalogSort.ReleaseDateDescending, maxAge);
        return Result<BrowseMoviesResult>.Success(await movies.BrowseAsync(criteria, cancellationToken));
    }
}
