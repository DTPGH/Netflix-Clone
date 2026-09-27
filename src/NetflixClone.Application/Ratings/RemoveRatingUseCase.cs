using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Ratings;
public interface IRemoveRatingUseCase
{
    Task<Result<RatingResult>> ExecuteAsync(RatingQuery query, CancellationToken ct = default);
}
public sealed class RemoveRatingUseCase(IProfileRepository profiles, IRatingRepository ratings, IUnitOfWork unitOfWork) : IRemoveRatingUseCase
{
    public async Task<Result<RatingResult>> ExecuteAsync(RatingQuery query, CancellationToken ct = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(query.UserAccountId, query.ProfileId, ct);
        if (profile is null || profile.IsDeleted) return Result<RatingResult>.Failure(RatingErrors.ProfileNotFound);
        // Existing ratings can be removed even after the movie is soft-deleted.
        var rating = await ratings.GetAsync(query.ProfileId, query.MovieId, ct);
        if (rating is null) return Result<RatingResult>.Success(new(query.MovieId, null));
        ratings.Remove(rating);
        try { await unitOfWork.SaveChangesAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<RatingResult>.Failure(RatingErrors.ConcurrentChange); }
        return Result<RatingResult>.Success(new(query.MovieId, null));
    }
}
