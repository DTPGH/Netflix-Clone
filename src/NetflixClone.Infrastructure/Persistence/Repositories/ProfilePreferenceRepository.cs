using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Repositories;
public sealed class ProfilePreferenceRepository(NetflixCloneDbContext db) : IProfilePreferenceRepository
{
    public async Task<IReadOnlyList<int>> GetMovieIdsAsync(int profileId, CancellationToken ct = default)
        => await db.ProfilePreferences.AsNoTracking().Where(p => p.ProfileId == profileId).OrderBy(p => p.MovieId).Select(p => p.MovieId).ToListAsync(ct);
    public async Task AddAsync(ProfilePreference preference, CancellationToken ct = default)
        => await db.ProfilePreferences.AddAsync(preference, ct);
}
