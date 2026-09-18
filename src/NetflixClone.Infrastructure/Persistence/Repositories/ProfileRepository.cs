using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Profiles;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Infrastructure.Persistence.Repositories;
public sealed class ProfileRepository(NetflixCloneDbContext dbContext) : IProfileRepository
{
    public async Task<IReadOnlyList<ProfileSummary>> ListActiveByAccountAsync(int userAccountId, CancellationToken cancellationToken = default)
        => await dbContext.Profiles.AsNoTracking()
            .Where(p => p.UserAccountId == userAccountId && !p.IsDeleted)
            .OrderBy(p => p.Id)
            .Select(p => new ProfileSummary(p.Id, p.Name, p.AvatarUrl, p.IsKids, p.MaturityLevel, p.OnboardingCompleted))
            .ToListAsync(cancellationToken);

    public Task<Profile?> GetByIdForAccountAsync(int userAccountId, int profileId, CancellationToken cancellationToken = default)
        => dbContext.Profiles.FirstOrDefaultAsync(p => p.UserAccountId == userAccountId && p.Id == profileId, cancellationToken);

    public Task<int> CountActiveByAccountAsync(int userAccountId, CancellationToken cancellationToken = default)
        => dbContext.Profiles.CountAsync(p => p.UserAccountId == userAccountId && !p.IsDeleted, cancellationToken);

    public async Task AddAsync(Profile profile, CancellationToken cancellationToken = default)
        => await dbContext.Profiles.AddAsync(profile, cancellationToken);
}
