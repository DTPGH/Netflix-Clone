using NetflixClone.Domain.Entities;

namespace NetflixClone.Application.Common.Abstractions.Persistence;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}