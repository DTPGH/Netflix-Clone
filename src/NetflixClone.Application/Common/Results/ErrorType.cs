namespace NetflixClone.Application.Common.Results;

public enum ErrorType
{
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden,
    Failure
}