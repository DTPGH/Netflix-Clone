using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.Profiles;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Viewing;
public interface IWatchProgressUseCase
{
    Task<Result<WatchHistoryPage>> ListAsync(int accountId, int profileId, int page, CancellationToken ct = default);
    Task<Result<WatchProgress>> GetAsync(ProfileMovieQuery query, CancellationToken ct = default);
    Task<Result<WatchProgress>> SaveAsync(SaveWatchProgress command, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ContinueWatchingItem>>> ContinueAsync(int accountId, int profileId, int limit, CancellationToken ct = default);
}
public sealed class WatchProgressUseCase(IGetMoviePlaybackUseCase playback, IProfileRepository profiles,
    IWatchHistoryRepository history, IUnitOfWork unitOfWork, IClock clock) : IWatchProgressUseCase
{
    public async Task<Result<WatchHistoryPage>> ListAsync(int accountId, int profileId, int page, CancellationToken ct = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(accountId, profileId, ct);
        if (profile is null || profile.IsDeleted)
            return Result<WatchHistoryPage>.Failure(WatchProgressErrors.ProfileNotFound);
        if (page is < 1 or > 1000000)
            return Result<WatchHistoryPage>.Failure(new("WatchHistory.InvalidPage", "Invalid history page.", ErrorType.Validation));
        return Result<WatchHistoryPage>.Success(await history.ListAsync(profileId, page, 20, ct));
    }
    private static WatchProgress Map(int movieId, WatchHistory? row) => new(movieId, row?.LastPositionSeconds ?? 0,
        row?.IsCompleted ?? false, row is null ? null : DateTime.SpecifyKind(row.UpdatedAt, DateTimeKind.Utc));
    public async Task<Result<WatchProgress>> GetAsync(ProfileMovieQuery query, CancellationToken ct = default)
    {
        var access = await playback.ExecuteAsync(query, ct);
        if (access.IsFailure) return Result<WatchProgress>.Failure(access.Error!);
        return Result<WatchProgress>.Success(Map(query.MovieId, await history.GetAsync(query.ProfileId, query.MovieId, ct)));
    }
    public async Task<Result<WatchProgress>> SaveAsync(SaveWatchProgress command, CancellationToken ct = default)
    {
        var access = await playback.ExecuteAsync(new(command.UserAccountId, command.ProfileId, command.MovieId), ct);
        if (access.IsFailure) return Result<WatchProgress>.Failure(access.Error!);
        // Duration is the actual demo asset duration reported by the player, not the catalog movie runtime.
        // This is convenience state, never proof of viewing for billing or analytics.
        if (command.DurationSeconds is < 1 or > 86400 || command.PositionSeconds < 0 ||
            command.PositionSeconds > command.DurationSeconds ||
            command.Ended && command.PositionSeconds < command.DurationSeconds - 2 ||
            command.ExpectedUpdatedAtUtc is { Kind: not DateTimeKind.Utc })
            return Result<WatchProgress>.Failure(WatchProgressErrors.Invalid);
        var row = await history.GetAsync(command.ProfileId, command.MovieId, ct);
        if (row?.UpdatedAt.Ticks != command.ExpectedUpdatedAtUtc?.Ticks)
            return Result<WatchProgress>.Failure(WatchProgressErrors.Conflict);
        var now = clock.UtcNow;
        if (row is null)
        {
            // Opening metadata or an initial zero-position checkpoint must not create history.
            if (command.PositionSeconds == 0 && !command.Ended)
                return Result<WatchProgress>.Success(Map(command.MovieId, null));
            row = new WatchHistory { ProfileId = command.ProfileId, MovieId = command.MovieId, CreatedAt = now, UpdatedAt = now };
            await history.AddAsync(row, ct);
        }
        else row.UpdatedAt = ProfileRules.NextUpdatedAt(row.UpdatedAt, now);
        row.LastPositionSeconds = command.PositionSeconds;
        row.LastWatchedAt = now;
        row.IsCompleted = command.Ended;
        row.CompletedAt = command.Ended ? row.CompletedAt ?? now : null;
        row.IsHidden = false; // An explicitly watched movie returns to Continue Watching.
        try { await unitOfWork.SaveChangesAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<WatchProgress>.Failure(WatchProgressErrors.Conflict); }
        return Result<WatchProgress>.Success(Map(command.MovieId, row));
    }
    public async Task<Result<IReadOnlyList<ContinueWatchingItem>>> ContinueAsync(int accountId, int profileId, int limit, CancellationToken ct = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(accountId, profileId, ct);
        if (profile is null || profile.IsDeleted)
            return Result<IReadOnlyList<ContinueWatchingItem>>.Failure(WatchProgressErrors.ProfileNotFound);
        if (limit is < 1 or > 24)
            return Result<IReadOnlyList<ContinueWatchingItem>>.Failure(WatchProgressErrors.Invalid);
        return Result<IReadOnlyList<ContinueWatchingItem>>.Success(await history.ListContinueAsync(profileId, limit, ct));
    }
}
