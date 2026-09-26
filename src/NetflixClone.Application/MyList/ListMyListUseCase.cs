using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.MyList;
public interface IListMyListUseCase
{
    Task<Result<MyListResult>> ExecuteAsync(ListMyListQuery query, CancellationToken cancellationToken = default);
}
public sealed class ListMyListUseCase(IProfileRepository profiles, IMyListRepository items) : IListMyListUseCase
{
    public async Task<Result<MyListResult>> ExecuteAsync(ListMyListQuery query, CancellationToken cancellationToken = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(query.UserAccountId, query.ProfileId, cancellationToken);
        if (profile is null || profile.IsDeleted) return Result<MyListResult>.Failure(MyListErrors.ProfileNotFound);
        if (query.Page < 1 || query.PageSize is < 1 or > 50 || (long)(query.Page - 1) * query.PageSize > int.MaxValue)
            return Result<MyListResult>.Failure(MyListErrors.InvalidPage);
        return Result<MyListResult>.Success(await items.ListAsync(query.ProfileId, query.Page, query.PageSize, cancellationToken));
    }
}
