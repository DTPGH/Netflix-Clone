using NetflixClone.Application.Catalog.Movies;
namespace NetflixClone.Application.MyList;
public sealed record ListMyListQuery(int UserAccountId, int ProfileId, int Page = 1, int PageSize = 20);
public sealed record MyListMovieCommand(int UserAccountId, int ProfileId, int MovieId);
public sealed record MyListResult(IReadOnlyList<MovieSummary> Items, int Page, int PageSize, int TotalCount);
public sealed record MyListStatus(bool IsInMyList);
public sealed record MyListMutationResult;
