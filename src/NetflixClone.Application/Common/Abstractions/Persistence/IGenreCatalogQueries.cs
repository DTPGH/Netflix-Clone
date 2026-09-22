using NetflixClone.Application.Catalog.Genres;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IGenreCatalogQueries
{
    Task<IReadOnlyList<GenreSummary>> ListAsync(CancellationToken cancellationToken = default);
}
