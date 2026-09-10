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

    public async Task<IReadOnlyCollection<string>> GetRoleNamesAsync(int userAccountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserAccountId == userAccountId)
            .Select(userRole => userRole.Role.Name)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserAccount userAccount, CancellationToken cancellationToken = default)
    {
        await _dbContext.UserAccounts.AddAsync(userAccount, cancellationToken);
    }

    public async Task<UserAccount?> GetByIdAsync(int userAccountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.UserAccounts
            .FirstOrDefaultAsync(x => x.Id == userAccountId, cancellationToken);
    }

    public Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.UserAccounts.FirstOrDefaultAsync(account => account.Email == email, cancellationToken);
    }
}
