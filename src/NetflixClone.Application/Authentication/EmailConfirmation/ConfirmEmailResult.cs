namespace NetflixClone.Application.Authentication.EmailConfirmation;

public sealed record ConfirmEmailResult(int UserAccountId, string Email, bool EmailConfirmed);