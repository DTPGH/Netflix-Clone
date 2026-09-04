using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository : IRoleRepository
{
    private readonly NetflixCloneDbContext _dbContext;
    public RoleRepository(NetflixCloneDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        return _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(role => role.Name == name, cancellationToken);
    }
}