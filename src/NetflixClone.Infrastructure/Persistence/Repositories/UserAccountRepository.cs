using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Infrastructure.Persistence.Repositories;

public sealed class UserAccountRepository : IUserAccountRepository
{
    private readonly NetflixCloneDbContext _dbContext;
    public UserAccountRepository(NetflixCloneDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return _dbContext.UserAccounts
            .AsNoTracking()
            .AnyAsync(
                x => x.Email == email, cancellationToken
            );
    }

    public async Task AddAsync(UserAccount userAccount, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserAccounts.AddAsync(userAccount, cancellationToken);
    }
}