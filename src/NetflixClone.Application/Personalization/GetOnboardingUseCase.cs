using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Personalization;
public interface IGetOnboardingUseCase
{
    Task<Result<OnboardingState>> ExecuteAsync(ProfileContext query, CancellationToken ct = default);
}
public sealed class GetOnboardingUseCase(IProfileRepository profiles, IProfilePreferenceRepository preferences) : IGetOnboardingUseCase
{
    public async Task<Result<OnboardingState>> ExecuteAsync(ProfileContext query, CancellationToken ct = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(query.UserAccountId, query.ProfileId, ct);
        if (profile is null || profile.IsDeleted) return Result<OnboardingState>.Failure(PersonalizationErrors.ProfileNotFound);
        return Result<OnboardingState>.Success(new(profile.OnboardingCompleted, await preferences.GetMovieIdsAsync(profile.Id, ct)));
    }
}
