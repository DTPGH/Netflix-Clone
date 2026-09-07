namespace NetflixClone.Application.Authentication.EmailConfirmation;

public sealed record ConfirmEmailCommand(int UserAccountId, string Token);