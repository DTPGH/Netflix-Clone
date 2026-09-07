using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.EmailConfirmation;

public static class ConfirmEmailErrors
{
    public static readonly Error InvalidOrExpiredToken = new(
        "Auth.EmailConfirmation.InvalidOrExpiredToken",
        "The email confirmation token is invalid or has expired.",
        ErrorType.Validation);
}