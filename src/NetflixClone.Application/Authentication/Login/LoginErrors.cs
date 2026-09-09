using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.Login;

public static class LoginErrors
{
    public static readonly Error InvalidCredentials = new(
        "Auth.Login.InvalidCredentials",
        "The email or password is incorrect.",
        ErrorType.Unauthorized);

    public static readonly Error AccountLocked = new(
        "Auth.Login.AccountLocked",
        "The account is locked.",
        ErrorType.Forbidden);

    public static readonly Error EmailNotConfirmed = new(
        "Auth.Login.EmailNotConfirmed",
        "The email address has not been confirmed.",
        ErrorType.Forbidden);

    public static readonly Error TemporarilyLocked = new(
        "Auth.Login.TemporarilyLocked",
        "The account is temporarily locked. Please try again later.",
        ErrorType.Unauthorized);
}
