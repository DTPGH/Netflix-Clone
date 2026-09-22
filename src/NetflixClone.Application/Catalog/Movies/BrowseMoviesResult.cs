namespace NetflixClone.Application.Catalog.Movies;
public sealed record BrowseMoviesResult(IReadOnlyList<MovieSummary> Items, int Page, int PageSize, int TotalCount);

