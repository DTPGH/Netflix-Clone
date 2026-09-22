using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Catalog.Movies;
public static class MovieErrors
{
    public static readonly Error InvalidQuery = new("Movies.InvalidQuery",
        "Use page >= 1, pageSize 1–50, search up to 100 characters, a positive genreId and sort releaseDateDesc or titleAsc.", ErrorType.Validation);
    public static readonly Error NotFound = new("Movies.NotFound", "The movie was not found.", ErrorType.NotFound);
}
