using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task<IReadOnlyList<RefreshToken>> GetActiveByDeviceAsync(int userAccountId, int deviceId, DateTime utcNow, CancellationToken cancellationToken = default);
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
