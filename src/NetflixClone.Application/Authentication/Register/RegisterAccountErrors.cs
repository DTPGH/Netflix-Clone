using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.Register;

public static class RegisterAccountErrors
{
    public static readonly Error EmailAlreadyExists = new(
        "Auth.Register.EmailAlreadyExists",
        "An account with this email already exists.",
        ErrorType.Conflict);

    public static readonly Error UserRoleNotFound = new(
        "Auth.Register.UserRoleNotFound",
        "The default User role is not configured.",
        ErrorType.Failure);
}