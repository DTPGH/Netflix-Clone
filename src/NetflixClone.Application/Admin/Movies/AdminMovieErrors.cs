using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Admin.Movies;

public static class AdminMovieErrors
{
    public static readonly Error InvalidPerson = new("AdminPeople.InvalidPerson", "Enter a name of at most 200 characters, a supported image URL of at most 500 characters, and a birth date that is not in the future.", ErrorType.Validation);
    public static readonly Error InvalidCredits = new("AdminMovies.InvalidCredits", "Select existing people, use Actor or Director, and do not repeat the same person and role. Character names are only for actors and must be at most 200 characters.", ErrorType.Validation);
    public static readonly Error InvalidQuery = new("AdminMovies.InvalidQuery", "The movie query is invalid.", ErrorType.Validation);
    public static readonly Error InvalidMovie = new("AdminMovies.InvalidMovie", "The movie data is invalid.", ErrorType.Validation);
    public static readonly Error InvalidGenres = new("AdminMovies.InvalidGenres", "Select one or more configured genres.", ErrorType.Validation);
    public static readonly Error NotFound = new("AdminMovies.NotFound", "The movie was not found.", ErrorType.NotFound);
    public static readonly Error AccountUnavailable = new("AdminMovies.AccountUnavailable", "The authenticated account is unavailable.", ErrorType.Unauthorized);
    public static readonly Error Forbidden = new("AdminMovies.Forbidden", "Current administrator access is required.", ErrorType.Forbidden);
    public static readonly Error ConcurrentChange = new("AdminMovies.ConcurrentChange", "The movie changed. Reload and try again.", ErrorType.Conflict);
    public static readonly Error NotDeleted = new("AdminMovies.NotDeleted", "The movie is already active.", ErrorType.Conflict);
}
