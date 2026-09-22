using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Catalog.Movies;
public sealed class BrowseMoviesUseCase(IMovieCatalogQueries movies) : IBrowseMoviesUseCase
{
    public async Task<Result<BrowseMoviesResult>> ExecuteAsync(BrowseMoviesQuery query, CancellationToken cancellationToken = default)
    {
        var search = query.Search?.Trim();
        if (query.Page < 1 || query.PageSize is < 1 or > 50 || search?.Length > 100 ||
            query.GenreId is <= 0 || (long)(query.Page - 1) * query.PageSize > int.MaxValue ||
            query.Sort is not ("releaseDateDesc" or "titleAsc"))
            return Result<BrowseMoviesResult>.Failure(MovieErrors.InvalidQuery);
        var criteria = new MovieCatalogCriteria(query.Page, query.PageSize,
            string.IsNullOrEmpty(search) ? null : search, query.GenreId,
            query.Sort == "titleAsc" ? MovieCatalogSort.TitleAscending : MovieCatalogSort.ReleaseDateDescending);
        return Result<BrowseMoviesResult>.Success(await movies.BrowseAsync(criteria, cancellationToken));
    }
}
