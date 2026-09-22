namespace NetflixClone.Api.Contracts.Catalog;
public sealed record BrowseMoviesResponse(IReadOnlyList<MovieSummaryResponse> Items, int Page, int PageSize, int TotalCount);
