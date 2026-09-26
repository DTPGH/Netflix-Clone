using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.MyList;
public interface IGetMyListStatusUseCase
{
    Task<Result<MyListStatus>> ExecuteAsync(MyListMovieCommand command, CancellationToken cancellationToken = default);
}
public sealed class GetMyListStatusUseCase(IProfileRepository profiles, IMyListRepository items) : IGetMyListStatusUseCase
{
    public async Task<Result<MyListStatus>> ExecuteAsync(MyListMovieCommand command, CancellationToken cancellationToken = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(command.UserAccountId, command.ProfileId, cancellationToken);
        if (profile is null || profile.IsDeleted) return Result<MyListStatus>.Failure(MyListErrors.ProfileNotFound);
        if (!await items.MovieExistsAsync(command.MovieId, cancellationToken))
            return Result<MyListStatus>.Failure(MyListErrors.MovieNotFound);
        return Result<MyListStatus>.Success(new(await items.GetAsync(command.ProfileId, command.MovieId, cancellationToken) is not null));
    }
}
