using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Repositories;
public sealed class RatingRepository(NetflixCloneDbContext dbContext) : IRatingRepository
{
    public Task<bool> MovieExistsAsync(int movieId, CancellationToken ct = default)
        => dbContext.Movies.AnyAsync(m => m.Id == movieId && !m.IsDeleted, ct);
    public Task<Rating?> GetAsync(int profileId, int movieId, CancellationToken ct = default)
        => dbContext.Ratings.SingleOrDefaultAsync(r => r.ProfileId == profileId && r.MovieId == movieId, ct);
    public async Task AddAsync(Rating rating, CancellationToken ct = default)
        => await dbContext.Ratings.AddAsync(rating, ct);
    public void Remove(Rating rating) => dbContext.Ratings.Remove(rating);
}
