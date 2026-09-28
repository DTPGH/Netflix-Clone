using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.Profiles;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Personalization;
public interface ICompleteOnboardingUseCase
{
    Task<Result<OnboardingState>> ExecuteAsync(CompleteOnboardingCommand command, CancellationToken ct = default);
}
public sealed class CompleteOnboardingUseCase(IProfileRepository profiles, IProfilePreferenceRepository preferences,
    IPersonalizationQueries movies, IUnitOfWork unitOfWork, IClock clock) : ICompleteOnboardingUseCase
{
    public async Task<Result<OnboardingState>> ExecuteAsync(CompleteOnboardingCommand command, CancellationToken ct = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(command.UserAccountId, command.ProfileId, ct);
        if (profile is null || profile.IsDeleted) return Result<OnboardingState>.Failure(PersonalizationErrors.ProfileNotFound);
        var ids = command.MovieIds;
        if (ids is null || ids.Count > PersonalizationRules.MaxSelections || ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count)
            return Result<OnboardingState>.Failure(PersonalizationErrors.InvalidSelection);
        var existing = await preferences.GetMovieIdsAsync(profile.Id, ct);
        // Compare stored IDs before eligibility: a retry must survive a later movie removal.
        if (profile.OnboardingCompleted)
            return existing.Order().SequenceEqual(ids.Order())
                ? Result<OnboardingState>.Success(new(true, existing))
                : Result<OnboardingState>.Failure(PersonalizationErrors.AlreadyCompleted);
        // Incomplete profiles should have no persisted preferences. Do not silently repair data.
        if (existing.Count != 0) return Result<OnboardingState>.Failure(PersonalizationErrors.ConcurrentChange);
        if (ids.Count > 0 && !await movies.AreMoviesEligibleAsync(ids, profile.MaturityLevel, ct))
            return Result<OnboardingState>.Failure(PersonalizationErrors.InvalidMovies);
        var now = clock.UtcNow;
        foreach (var id in ids)
            await preferences.AddAsync(new ProfilePreference { ProfileId = profile.Id, MovieId = id, CreatedAt = now }, ct);
        profile.OnboardingCompleted = true;
        profile.UpdatedAt = ProfileRules.NextUpdatedAt(profile.UpdatedAt, now);
        try { await unitOfWork.SaveChangesAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<OnboardingState>.Failure(PersonalizationErrors.ConcurrentChange); }
        return Result<OnboardingState>.Success(new(true, ids.Order().ToArray()));
    }
}
