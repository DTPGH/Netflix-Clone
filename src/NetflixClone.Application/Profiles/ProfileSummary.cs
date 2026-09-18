using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Profiles;
public sealed record ProfileSummary(int ProfileId, string Name, string? AvatarUrl, bool IsKids,
    byte MaturityLevel, bool OnboardingCompleted)
{
    public static ProfileSummary From(Profile profile) => new(profile.Id, profile.Name, profile.AvatarUrl,
        profile.IsKids, profile.MaturityLevel, profile.OnboardingCompleted);
}
