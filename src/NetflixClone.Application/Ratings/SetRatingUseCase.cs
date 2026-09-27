using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Ratings;
public interface ISetRatingUseCase
{
    Task<Result<RatingResult>> ExecuteAsync(SetRatingCommand command, CancellationToken ct = default);
}
public sealed class SetRatingUseCase(IProfileRepository profiles, IRatingRepository ratings, IUnitOfWork unitOfWork, IClock clock) : ISetRatingUseCase
{
    public async Task<Result<RatingResult>> ExecuteAsync(SetRatingCommand command, CancellationToken ct = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(command.UserAccountId, command.ProfileId, ct);
        if (profile is null || profile.IsDeleted) return Result<RatingResult>.Failure(RatingErrors.ProfileNotFound);
        if (!RatingRules.IsValid(command.Value)) return Result<RatingResult>.Failure(RatingErrors.InvalidValue);
        if (!await ratings.MovieExistsAsync(command.MovieId, ct)) return Result<RatingResult>.Failure(RatingErrors.MovieNotFound);
        var rating = await ratings.GetAsync(command.ProfileId, command.MovieId, ct);
        if (rating is null)
        {
            var now = clock.UtcNow;
            await ratings.AddAsync(new Rating { ProfileId = command.ProfileId, MovieId = command.MovieId,
                Value = command.Value!, CreatedAt = now, UpdatedAt = now }, ct);
        }
        else
        {
            if (rating.Value == command.Value) return Result<RatingResult>.Success(new(command.MovieId, rating.Value));
            rating.Value = command.Value!;
            rating.UpdatedAt = RatingRules.NextUpdatedAt(rating.UpdatedAt, clock.UtcNow);
        }
        try { await unitOfWork.SaveChangesAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<RatingResult>.Failure(RatingErrors.ConcurrentChange); }
        return Result<RatingResult>.Success(new(command.MovieId, command.Value));
    }
}
