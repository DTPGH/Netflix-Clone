namespace NetflixClone.Api.Contracts.Viewing;
public sealed record WatchProgressRequest(int PositionSeconds, int DurationSeconds, bool Ended, DateTime? ExpectedUpdatedAtUtc);
