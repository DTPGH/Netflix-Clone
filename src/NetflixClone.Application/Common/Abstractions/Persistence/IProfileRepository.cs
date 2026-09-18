using NetflixClone.Application.Profiles;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IProfileRepository
{
    Task<IReadOnlyList<ProfileSummary>> ListActiveByAccountAsync(int userAccountId, CancellationToken cancellationToken = default);
    Task<Profile?> GetByIdForAccountAsync(int userAccountId, int profileId, CancellationToken cancellationToken = default);
    Task<int> CountActiveByAccountAsync(int userAccountId, CancellationToken cancellationToken = default);
    Task AddAsync(Profile profile, CancellationToken cancellationToken = default);
}
