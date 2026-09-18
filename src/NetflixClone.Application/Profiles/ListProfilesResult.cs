namespace NetflixClone.Application.Profiles;
public sealed record ListProfilesResult(IReadOnlyList<ProfileSummary> Profiles);
