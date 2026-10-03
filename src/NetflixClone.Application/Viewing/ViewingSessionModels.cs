using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Viewing;

public sealed record ViewingSessionReply(int SessionId, int WatchedSeconds, bool IsQualifiedView, bool IsEnded, long Sequence);
public sealed record ViewingCheckpoint(long Sequence, long WatchedMilliseconds, string? EndReason);
// Persistence metadata is kept separate from the scaffolded entity.
public sealed class ViewingSessionState
{
    public required ViewingSession Session { get; init; }
    public Guid? ClientSessionId { get; init; }
    public long Sequence { get; set; }
    public long WatchedMilliseconds { get; set; }
    public DateTime LastCheckpointAtUtc { get; set; }
}
