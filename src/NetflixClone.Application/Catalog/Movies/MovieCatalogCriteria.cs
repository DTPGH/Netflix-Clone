namespace NetflixClone.Application.Catalog.Movies;
public enum MovieCatalogSort { ReleaseDateDescending, TitleAscending }
public sealed record MovieCatalogCriteria(int Page, int PageSize, string? Search, int? GenreId, MovieCatalogSort Sort);

