using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.MyList;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Repositories;
public sealed class MyListRepository(NetflixCloneDbContext db) : IMyListRepository
{
    public async Task<MyListResult> ListAsync(int profileId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.MyListItems.AsNoTracking().Where(x => x.ProfileId == profileId && !x.Movie.IsDeleted && !x.Profile.IsDeleted);
        var count = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip(checked((page - 1) * pageSize)).Take(pageSize)
            .Select(x => new MovieSummary(x.Movie.Id, x.Movie.Title, x.Movie.ReleaseDate, x.Movie.DurationSeconds,
                x.Movie.ThumbnailUrl, x.Movie.MaturityRating, x.Movie.IsFeatured, x.Movie.IsAvailable))
            .ToListAsync(cancellationToken);
        return new(items, page, pageSize, count);
    }
    public Task<bool> MovieExistsAsync(int movieId, CancellationToken cancellationToken = default)
        => db.Movies.AsNoTracking().AnyAsync(m => m.Id == movieId && !m.IsDeleted, cancellationToken);
    public Task<MyListItem?> GetAsync(int profileId, int movieId, CancellationToken cancellationToken = default)
        => db.MyListItems.SingleOrDefaultAsync(x => x.ProfileId == profileId && x.MovieId == movieId, cancellationToken);
    public async Task AddAsync(MyListItem item, CancellationToken cancellationToken = default)
        => await db.MyListItems.AddAsync(item, cancellationToken);
    public void Remove(MyListItem item) => db.MyListItems.Remove(item);
}
