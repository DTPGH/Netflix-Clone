using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IProfilePreferenceRepository
{
    Task<IReadOnlyList<int>> GetMovieIdsAsync(int profileId, CancellationToken ct = default);
    Task AddAsync(ProfilePreference preference, CancellationToken ct = default);
}
