using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IRatingRepository
{
    Task<bool> MovieExistsAsync(int movieId, CancellationToken ct = default);
    Task<Rating?> GetAsync(int profileId, int movieId, CancellationToken ct = default);
    Task AddAsync(Rating rating, CancellationToken ct = default);
    void Remove(Rating rating);
}
