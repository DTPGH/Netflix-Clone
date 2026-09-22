using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Catalog.Movies;
public interface IBrowseMoviesUseCase
{
    Task<Result<BrowseMoviesResult>> ExecuteAsync(BrowseMoviesQuery query, CancellationToken cancellationToken = default);
}
