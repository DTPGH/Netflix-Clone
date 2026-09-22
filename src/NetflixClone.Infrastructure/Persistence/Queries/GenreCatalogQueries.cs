using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Catalog.Genres;
using NetflixClone.Application.Common.Abstractions.Persistence;
namespace NetflixClone.Infrastructure.Persistence.Queries;
public sealed class GenreCatalogQueries(NetflixCloneDbContext dbContext) : IGenreCatalogQueries
{
    public async Task<IReadOnlyList<GenreSummary>> ListAsync(CancellationToken cancellationToken = default)
        => await dbContext.Genres.AsNoTracking().OrderBy(g => g.Name).ThenBy(g => g.Id)
            .Select(g => new GenreSummary(g.Id, g.Name)).ToListAsync(cancellationToken);
}
