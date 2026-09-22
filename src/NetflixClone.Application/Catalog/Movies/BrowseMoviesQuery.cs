namespace NetflixClone.Application.Catalog.Movies;
public sealed record BrowseMoviesQuery(int Page = 1, int PageSize = 20, string? Search = null, int? GenreId = null, string? Sort = "releaseDateDesc");

