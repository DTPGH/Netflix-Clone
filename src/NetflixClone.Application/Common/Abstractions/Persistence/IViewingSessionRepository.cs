using NetflixClone.Application.Viewing;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IViewingSessionRepository
{
    Task<ViewingSessionState?> GetAsync(int accountId, int profileId, int movieId, int deviceId, Guid clientSessionId, CancellationToken ct);
    Task<ViewingSessionState?> GetAsync(int accountId, int profileId, int movieId, int deviceId, int sessionId, CancellationToken ct);
    Task<IReadOnlyList<ViewingSessionState>> GetStaleAsync(int deviceId, DateTime cutoffUtc, CancellationToken ct);
    void Add(ViewingSessionState state);
    void Update(ViewingSessionState state);
}
public interface IViewingSessionScope : IAsyncDisposable
{
    int DeviceId { get; }
    Task CommitAsync(CancellationToken ct);
}
public interface IViewingSessionScopeFactory
{
    Task<IViewingSessionScope?> BeginAsync(int accountId, string deviceHash, CancellationToken ct);
}
