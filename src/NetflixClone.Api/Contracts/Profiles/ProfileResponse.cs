using NetflixClone.Application.Profiles;
namespace NetflixClone.Api.Contracts.Profiles;
public sealed record ProfileResponse(int ProfileId, string Name, string? AvatarUrl, bool IsKids,
    byte MaturityLevel, bool OnboardingCompleted)
{
    public static ProfileResponse From(ProfileSummary profile) => new(profile.ProfileId, profile.Name,
        profile.AvatarUrl, profile.IsKids, profile.MaturityLevel, profile.OnboardingCompleted);
}
