using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
}
