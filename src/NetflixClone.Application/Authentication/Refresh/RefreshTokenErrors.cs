using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Authentication.Refresh;

public static class RefreshTokenErrors
{
    public static readonly Error InvalidRefreshToken = new(
        "Auth.RefreshToken.InvalidRefreshToken", "The refresh token is invalid.", ErrorType.Unauthorized);
    public static readonly Error RolesNotConfigured = new(
        "Auth.RefreshToken.RolesNotConfigured", "The account has no assigned roles.", ErrorType.Failure);
}
