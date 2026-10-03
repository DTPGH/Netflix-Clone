using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Viewing;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Repositories;
public sealed class ViewingSessionRepository(NetflixCloneDbContext db) : IViewingSessionRepository
{
    private IQueryable<ViewingSession> Owned(int accountId, int profileId, int movieId, int deviceId) => db.ViewingSessions.Where(s =>
        s.ProfileId == profileId && s.MovieId == movieId && s.DeviceId == deviceId && s.Profile.UserAccountId == accountId && s.Device.UserAccountId == accountId);
    private ViewingSessionState Read(ViewingSession row)
    {
        var entry = db.Entry(row);
        return new() { Session = row, ClientSessionId = entry.Property<Guid?>("ClientSessionId").CurrentValue,
            Sequence = entry.Property<long>("CheckpointSequence").CurrentValue,
            WatchedMilliseconds = entry.Property<long>("WatchedMilliseconds").CurrentValue,
            LastCheckpointAtUtc = DateTime.SpecifyKind(entry.Property<DateTime>("LastCheckpointAtUtc").CurrentValue, DateTimeKind.Utc) };
    }
    public async Task<ViewingSessionState?> GetAsync(int accountId, int profileId, int movieId, int deviceId, Guid clientSessionId, CancellationToken ct)
    {
        // Read by the complete unique key first; a reused key for another movie/profile is rejected by Application.
        var row = await db.ViewingSessions.SingleOrDefaultAsync(s => s.DeviceId == deviceId &&
            s.Device.UserAccountId == accountId && EF.Property<Guid?>(s, "ClientSessionId") == clientSessionId, ct);
        return row is null ? null : Read(row);
    }
    public async Task<ViewingSessionState?> GetAsync(int accountId, int profileId, int movieId, int deviceId, int sessionId, CancellationToken ct)
    {
        var row = await Owned(accountId, profileId, movieId, deviceId).SingleOrDefaultAsync(s => s.Id == sessionId, ct);
        return row is null ? null : Read(row);
    }
    public async Task<IReadOnlyList<ViewingSessionState>> GetStaleAsync(int deviceId, DateTime cutoffUtc, CancellationToken ct)
        => (await db.ViewingSessions.Where(s => s.DeviceId == deviceId && s.EndedAt == null &&
            EF.Property<DateTime>(s, "LastCheckpointAtUtc") <= cutoffUtc).ToListAsync(ct)).Select(Read).ToArray();
    public void Add(ViewingSessionState state) { db.ViewingSessions.Add(state.Session); Update(state); }
    public void Update(ViewingSessionState state)
    {
        var entry = db.Entry(state.Session);
        entry.Property<Guid?>("ClientSessionId").CurrentValue = state.ClientSessionId;
        entry.Property<long>("CheckpointSequence").CurrentValue = state.Sequence;
        entry.Property<long>("WatchedMilliseconds").CurrentValue = state.WatchedMilliseconds;
        entry.Property<DateTime>("LastCheckpointAtUtc").CurrentValue = state.LastCheckpointAtUtc;
    }
}
