using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Viewing;
public sealed record WatchProgress(int MovieId, int PositionSeconds, bool IsCompleted, DateTime? UpdatedAtUtc);
public sealed record SaveWatchProgress(int UserAccountId, int ProfileId, int MovieId, int PositionSeconds,
    int DurationSeconds, bool Ended, DateTime? ExpectedUpdatedAtUtc);
public sealed record ContinueWatchingItem(MovieSummary Movie, int PositionSeconds, DateTime LastWatchedAtUtc);
public static class WatchProgressErrors
{
    public static readonly Error Invalid = new("WatchHistory.InvalidProgress", "Invalid video position or duration.", ErrorType.Validation);
    public static readonly Error Conflict = new("WatchHistory.ConcurrentChange", "Progress changed. Reload playback before saving again.", ErrorType.Conflict);
    public static readonly Error ProfileNotFound = new("WatchHistory.ProfileNotFound", "The profile was not found.", ErrorType.NotFound);
}
