namespace NetflixClone.Application.Profiles;
public sealed record CreateProfileCommand(int UserAccountId, string Name, bool IsKids);
