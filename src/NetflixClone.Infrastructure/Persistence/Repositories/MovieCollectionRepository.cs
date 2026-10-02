using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Collections;
using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Repositories;
public sealed class MovieCollectionRepository(NetflixCloneDbContext db) : IMovieCollectionRepository, IMovieCollectionQueries
{
    public async Task<CollectionPage> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        var query = db.MovieCollections.AsNoTracking();
        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new CollectionSummary(c.Id, c.Title, c.IsPublished, c.DisplayOrder, c.MovieCollectionItems.Count, c.UpdatedAt)).ToListAsync(ct);
        return new(items.Select(c => c with { UpdatedAtUtc = DateTime.SpecifyKind(c.UpdatedAtUtc, DateTimeKind.Utc) }).ToArray(), page, pageSize, count);
    }
    public Task<MovieCollection?> GetAsync(int id, CancellationToken ct) => db.MovieCollections
        .Include(c => c.MovieCollectionItems).ThenInclude(i => i.Movie).AsSplitQuery().SingleOrDefaultAsync(c => c.Id == id, ct);
    public async Task<IReadOnlyList<Movie>> GetMoviesAsync(IReadOnlyList<int> ids, CancellationToken ct)
        => await db.Movies.Where(m => ids.Contains(m.Id)).ToListAsync(ct);
    public async Task AddAsync(MovieCollection collection, CancellationToken ct) => await db.MovieCollections.AddAsync(collection, ct);
    public void RemoveItem(MovieCollectionItem item) => db.MovieCollectionItems.Remove(item);
    public async Task<IReadOnlyList<BrowseCollection>> BrowseAsync(byte maturityLevel, CancellationToken ct)
        => await db.MovieCollections.AsNoTracking().Where(c => c.IsPublished &&
                c.MovieCollectionItems.Any(i => !i.Movie.IsDeleted && i.Movie.MinAge <= maturityLevel))
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id)
            .Select(c => new BrowseCollection(c.Id, c.Title, c.MovieCollectionItems
                .Where(i => !i.Movie.IsDeleted && i.Movie.MinAge <= maturityLevel)
                .OrderBy(i => i.Position).ThenBy(i => i.MovieId)
                .Select(i => new MovieSummary(i.MovieId, i.Movie.Title, i.Movie.ReleaseDate, i.Movie.DurationSeconds,
                    i.Movie.ThumbnailUrl, i.Movie.MaturityRating, i.Movie.IsFeatured, i.Movie.IsAvailable)).ToArray()))
            .ToListAsync(ct);
}
