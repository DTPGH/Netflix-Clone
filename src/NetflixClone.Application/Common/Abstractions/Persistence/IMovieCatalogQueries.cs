using NetflixClone.Application.Catalog.Movies;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IMovieCatalogQueries
{
    Task<MoviePlayback?> GetPlaybackAsync(int movieId, CancellationToken cancellationToken = default);
    Task<BrowseMoviesResult> BrowseAsync(MovieCatalogCriteria criteria, CancellationToken cancellationToken = default);
    Task<MovieDetail?> GetDetailAsync(int movieId, CancellationToken cancellationToken = default);
}
