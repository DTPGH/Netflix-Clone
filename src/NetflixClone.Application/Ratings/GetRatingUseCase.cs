using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Ratings;
public interface IGetRatingUseCase
{
    Task<Result<RatingResult>> ExecuteAsync(RatingQuery query, CancellationToken ct = default);
}
public sealed class GetRatingUseCase(IProfileRepository profiles, IRatingRepository ratings) : IGetRatingUseCase
{
    public async Task<Result<RatingResult>> ExecuteAsync(RatingQuery query, CancellationToken ct = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(query.UserAccountId, query.ProfileId, ct);
        if (profile is null || profile.IsDeleted) return Result<RatingResult>.Failure(RatingErrors.ProfileNotFound);
        if (!await ratings.MovieExistsAsync(query.MovieId, ct)) return Result<RatingResult>.Failure(RatingErrors.MovieNotFound);
        var rating = await ratings.GetAsync(query.ProfileId, query.MovieId, ct);
        return Result<RatingResult>.Success(new(query.MovieId, rating?.Value));
    }
}
