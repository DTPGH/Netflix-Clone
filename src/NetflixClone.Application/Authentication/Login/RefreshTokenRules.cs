namespace NetflixClone.Application.Authentication.Login;

public static class RefreshTokenRules
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);
}
