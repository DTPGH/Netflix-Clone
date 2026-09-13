using NetflixClone.Domain.Entities;

namespace NetflixClone.Application.Common.Abstractions.Persistence;

public interface IUserAccountRepository
{
    Task<IReadOnlyCollection<string>> GetRoleNamesAsync(int userAccountId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task AddAsync(UserAccount userAccount, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByIdAsync(int userAccountId, CancellationToken cancellationToken = default);

    Task<UserAccount?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
