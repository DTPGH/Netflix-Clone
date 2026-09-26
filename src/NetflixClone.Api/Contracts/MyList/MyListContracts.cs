using NetflixClone.Api.Contracts.Catalog;
namespace NetflixClone.Api.Contracts.MyList;
public sealed class MyListPageRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
public sealed record MyListResponse(IReadOnlyList<MovieSummaryResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record MyListStatusResponse(bool IsInMyList);
