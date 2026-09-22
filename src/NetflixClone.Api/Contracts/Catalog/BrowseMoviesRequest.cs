namespace NetflixClone.Api.Contracts.Catalog;
public sealed class BrowseMoviesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public int? GenreId { get; set; }
    public string? Sort { get; set; } = "releaseDateDesc";
}
