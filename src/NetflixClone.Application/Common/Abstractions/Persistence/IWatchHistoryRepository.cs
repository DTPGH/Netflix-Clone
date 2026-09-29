using NetflixClone.Application.Viewing;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IWatchHistoryRepository
{
    Task<WatchHistory?> GetAsync(int profileId, int movieId, CancellationToken ct = default);
    Task AddAsync(WatchHistory history, CancellationToken ct = default);
    Task<IReadOnlyList<ContinueWatchingItem>> ListContinueAsync(int profileId, int limit, CancellationToken ct = default);
}
