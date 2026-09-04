namespace NetflixClone.Api.Contracts.Authentication;

public sealed record RegisterResponse(
    int UserAccountId,
    string Email,
    bool EmailConfirmed);