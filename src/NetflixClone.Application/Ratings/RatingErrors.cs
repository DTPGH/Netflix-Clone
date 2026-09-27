using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Ratings;
public static class RatingErrors
{
    public static readonly Error ProfileNotFound = new("Ratings.ProfileNotFound", "The profile was not found.", ErrorType.NotFound);
    public static readonly Error MovieNotFound = new("Ratings.MovieNotFound", "The movie was not found.", ErrorType.NotFound);
    public static readonly Error InvalidValue = new("Ratings.InvalidValue", "Value must be NotForMe, Like or Love.", ErrorType.Validation);
    public static readonly Error ConcurrentChange = new("Ratings.ConcurrentChange", "The rating changed. Reload and try again.", ErrorType.Conflict);
}
