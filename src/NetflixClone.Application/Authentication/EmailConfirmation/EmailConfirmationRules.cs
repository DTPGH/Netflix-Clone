namespace NetflixClone.Application.Authentication.EmailConfirmation;

public static class EmailConfirmationRules
{
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);
}