namespace NetflixClone.Application.Authentication.Register;

public sealed record RegisterAccountResult(int UserAccountId, string Email, bool EmailConfirmed);