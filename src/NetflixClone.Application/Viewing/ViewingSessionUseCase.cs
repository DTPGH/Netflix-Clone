using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Viewing;

public interface IViewingSessionUseCase
{
    Task<bool> IsActiveForPlaybackAsync(int accountId, int profileId, int movieId, int sessionId, CancellationToken ct);
    Task<Result<ViewingSessionReply>> StartAsync(int accountId, int profileId, int movieId, string? deviceIdentifier, Guid clientSessionId, CancellationToken ct);
    Task<Result<ViewingSessionReply>> CheckpointAsync(int accountId, int profileId, int movieId, string? deviceIdentifier, int sessionId, ViewingCheckpoint checkpoint, CancellationToken ct);
}
public sealed class ViewingSessionUseCase(IGetMoviePlaybackUseCase playback, IDeviceIdentifierService identifiers,
    IViewingSessionScopeFactory scopes, IViewingSessionRepository sessions, IUnitOfWork unitOfWork, IClock clock,
    ISubscriptionRepository subscriptions) : IViewingSessionUseCase
{
    public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);
    public static readonly Error StreamLimitReached = new("Playback.ConcurrentStreamLimitReached", "Your plan's simultaneous playback limit has been reached. Stop another stream or try again shortly.", ErrorType.Conflict);
    public static readonly Error SessionExpired = new("Playback.SessionExpired", "The playback slot expired. Request a new playback session.", ErrorType.Conflict);
    private static readonly Error Invalid = new("ViewingSessions.Invalid", "Invalid viewing checkpoint.", ErrorType.Validation);
    private static readonly Error Missing = new("ViewingSessions.NotFound", "Viewing session or device is unavailable.", ErrorType.NotFound);
    private static readonly Error Conflict = new("ViewingSessions.ConcurrentChange", "Viewing session changed. Retry the same checkpoint.", ErrorType.Conflict);
    private static ViewingSessionReply Map(ViewingSessionState state) => new(state.Session.Id, state.Session.WatchedSeconds,
        state.Session.IsQualifiedView, state.Session.EndedAt is not null, state.Sequence,
        DateTime.SpecifyKind(state.LastCheckpointAtUtc.Add(Timeout), DateTimeKind.Utc), state.WatchedMilliseconds);
    public Task<bool> IsActiveForPlaybackAsync(int accountId, int profileId, int movieId, int sessionId, CancellationToken ct)
        => sessionId > 0 ? sessions.IsActiveForPlaybackAsync(accountId, profileId, movieId, sessionId, clock.UtcNow - Timeout, ct) : Task.FromResult(false);
    private async Task ExpireAsync(int deviceId, DateTime now, CancellationToken ct)
    {
        foreach (var state in await sessions.GetStaleAsync(deviceId, now - Timeout, ct))
        {
            state.Session.EndedAt = state.LastCheckpointAtUtc;
            state.Session.EndReason = "Timeout";
            sessions.Update(state);
        }
    }
    public async Task<Result<ViewingSessionReply>> StartAsync(int accountId, int profileId, int movieId, string? deviceIdentifier, Guid clientSessionId, CancellationToken ct)
    {
        var hash = identifiers.HashIfValid(deviceIdentifier);
        if (hash is null || clientSessionId == Guid.Empty) return Result<ViewingSessionReply>.Failure(Invalid);
        await using var scope = await scopes.BeginAsync(accountId, hash, ct);
        if (scope is null) return Result<ViewingSessionReply>.Failure(Missing);
        var access = await playback.ExecuteAsync(new(accountId, profileId, movieId), ct);
        if (access.IsFailure) return Result<ViewingSessionReply>.Failure(access.Error!);
        var now = clock.UtcNow;
        await ExpireAsync(scope.DeviceId, now, ct);
        var state = await sessions.GetAsync(accountId, profileId, movieId, scope.DeviceId, clientSessionId, ct);
        if (state is not null && (state.Session.ProfileId != profileId || state.Session.MovieId != movieId))
            return Result<ViewingSessionReply>.Failure(Invalid);
        if (state is null)
        {
            // The scope holds the account UPDLOCK through count + insert + commit.
            // Read the current Plan; selling status does not cancel an already purchased subscription.
            var effective = (await subscriptions.GetActiveRowsAsync(accountId, ct))
                .Where(s => s.StartDate <= now && s.EndDate > now).ToArray();
            if (effective.Length == 0)
                return Result<ViewingSessionReply>.Failure(new("Movies.SubscriptionRequired", "An active subscription is required to play this video.", ErrorType.Forbidden));
            if (effective.Length != 1 || effective[0].Plan.MaxConcurrentStreams < 1)
                return Result<ViewingSessionReply>.Failure(new("Playback.InvalidEntitlement", "The playback entitlement is unavailable.", ErrorType.Failure));
            if (await sessions.CountActiveByAccountAsync(accountId, now - Timeout, ct) >= effective[0].Plan.MaxConcurrentStreams)
                return Result<ViewingSessionReply>.Failure(StreamLimitReached);
            state = new() { Session = new ViewingSession { ProfileId = profileId, MovieId = movieId,
                DeviceId = scope.DeviceId, StartedAt = now }, ClientSessionId = clientSessionId, LastCheckpointAtUtc = now };
            sessions.Add(state);
        }
        try { await unitOfWork.SaveChangesAsync(ct); await scope.CommitAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<ViewingSessionReply>.Failure(Conflict); }
        return Result<ViewingSessionReply>.Success(Map(state));
    }
    public async Task<Result<ViewingSessionReply>> CheckpointAsync(int accountId, int profileId, int movieId, string? deviceIdentifier,
        int sessionId, ViewingCheckpoint checkpoint, CancellationToken ct)
    {
        var hash = identifiers.HashIfValid(deviceIdentifier);
        if (hash is null || checkpoint.Sequence < 1 || checkpoint.WatchedMilliseconds < 0 ||
            checkpoint.EndReason is not (null or "Finished" or "Closed")) return Result<ViewingSessionReply>.Failure(Invalid);
        await using var scope = await scopes.BeginAsync(accountId, hash, ct);
        if (scope is null) return Result<ViewingSessionReply>.Failure(Missing);
        var access = await playback.ExecuteAsync(new(accountId, profileId, movieId), ct);
        if (access.IsFailure) return Result<ViewingSessionReply>.Failure(access.Error!);
        var state = await sessions.GetAsync(accountId, profileId, movieId, scope.DeviceId, sessionId, ct);
        if (state is null) return Result<ViewingSessionReply>.Failure(Missing);
        var now = clock.UtcNow;
        if (state.Session.EndedAt is not null || checkpoint.Sequence <= state.Sequence)
            return Result<ViewingSessionReply>.Success(Map(state));
        if (now - state.LastCheckpointAtUtc >= Timeout)
        {
            state.Session.EndedAt = state.LastCheckpointAtUtc; state.Session.EndReason = "Timeout";
        }
        else
        {
            // Cumulative counters make retries idempotent. Reject impossible additions instead of granting elapsed offline time.
            var delta = checkpoint.WatchedMilliseconds - state.WatchedMilliseconds;
            var elapsed = Math.Max(0, (long)(now - state.LastCheckpointAtUtc).TotalMilliseconds);
            if (delta < 0 || delta > elapsed + 2000 || checkpoint.WatchedMilliseconds > (long)(now - state.Session.StartedAt).TotalMilliseconds + 2000 ||
                checkpoint.WatchedMilliseconds > (long)int.MaxValue * 1000)
                return Result<ViewingSessionReply>.Failure(Invalid);
            state.WatchedMilliseconds = checkpoint.WatchedMilliseconds;
            state.Sequence = checkpoint.Sequence;
            // A new sequence with no viewing progress must not keep a paused slot alive.
            if (delta > 0) state.LastCheckpointAtUtc = now;
            state.Session.WatchedSeconds = (int)(state.WatchedMilliseconds / 1000);
            state.Session.IsQualifiedView |= state.Session.WatchedSeconds >= 120;
            if (checkpoint.EndReason is not null) { state.Session.EndedAt = now; state.Session.EndReason = checkpoint.EndReason; }
        }
        sessions.Update(state);
        try { await unitOfWork.SaveChangesAsync(ct); await scope.CommitAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<ViewingSessionReply>.Failure(Conflict); }
        return Result<ViewingSessionReply>.Success(Map(state));
    }
}
