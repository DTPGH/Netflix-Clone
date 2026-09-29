using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Viewing;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Repositories;
public sealed class WatchHistoryRepository(NetflixCloneDbContext db) : IWatchHistoryRepository
{
    public Task<WatchHistory?> GetAsync(int profileId, int movieId, CancellationToken ct = default)
        => db.WatchHistories.SingleOrDefaultAsync(h => h.ProfileId == profileId && h.MovieId == movieId, ct);
    public async Task AddAsync(WatchHistory history, CancellationToken ct = default) => await db.WatchHistories.AddAsync(history, ct);
    public async Task<IReadOnlyList<ContinueWatchingItem>> ListContinueAsync(int profileId, int limit, CancellationToken ct = default)
    {
        var items = await db.WatchHistories.AsNoTracking().Where(h => h.ProfileId == profileId &&
            !h.Profile.IsDeleted && !h.IsCompleted && !h.IsHidden && h.LastPositionSeconds > 0 &&
            !h.Movie.IsDeleted && h.Movie.IsAvailable && h.Movie.MinAge <= h.Profile.MaturityLevel)
            .OrderByDescending(h => h.LastWatchedAt).ThenByDescending(h => h.Id).Take(limit)
            .Select(h => new ContinueWatchingItem(
                new MovieSummary(h.Movie.Id, h.Movie.Title, h.Movie.ReleaseDate, h.Movie.DurationSeconds,
                    h.Movie.ThumbnailUrl, h.Movie.MaturityRating, h.Movie.IsFeatured, h.Movie.IsAvailable),
                h.LastPositionSeconds, h.LastWatchedAt)).ToListAsync(ct);
        return items.Select(i => i with { LastWatchedAtUtc = DateTime.SpecifyKind(i.LastWatchedAtUtc, DateTimeKind.Utc) }).ToArray();
    }
}
