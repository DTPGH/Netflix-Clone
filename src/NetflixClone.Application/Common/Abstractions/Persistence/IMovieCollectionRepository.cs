using NetflixClone.Application.Collections;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IMovieCollectionRepository
{
    Task<CollectionPage> ListAsync(int page, int pageSize, CancellationToken ct);
    Task<MovieCollection?> GetAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Movie>> GetMoviesAsync(IReadOnlyList<int> ids, CancellationToken ct);
    Task AddAsync(MovieCollection collection, CancellationToken ct);
    void RemoveItem(MovieCollectionItem item);
}
public interface IMovieCollectionQueries
{
    Task<IReadOnlyList<BrowseCollection>> BrowseAsync(byte maturityLevel, CancellationToken ct);
}
