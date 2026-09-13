using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace NetflixClone.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly NetflixCloneDbContext _dbContext;

    public RefreshTokenRepository(NetflixCloneDbContext dbContext) => _dbContext = dbContext;

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .Include(token => token.Device)
            .Include(token => token.UserAccount)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }
}
