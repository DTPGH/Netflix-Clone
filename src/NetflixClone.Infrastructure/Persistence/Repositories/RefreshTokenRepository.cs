using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly NetflixCloneDbContext _dbContext;

    public RefreshTokenRepository(NetflixCloneDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }
}
