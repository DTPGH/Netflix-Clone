namespace NetflixClone.Application.Profiles;
public sealed record UpdateProfileCommand(int UserAccountId, int ProfileId, string Name, bool IsKids);
