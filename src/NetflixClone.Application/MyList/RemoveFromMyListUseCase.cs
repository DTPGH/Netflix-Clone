using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.MyList;
public interface IRemoveFromMyListUseCase
{
    Task<Result<MyListMutationResult>> ExecuteAsync(MyListMovieCommand command, CancellationToken cancellationToken = default);
}
public sealed class RemoveFromMyListUseCase(IProfileRepository profiles, IMyListRepository items,
    IUnitOfWork unitOfWork) : IRemoveFromMyListUseCase
{
    public async Task<Result<MyListMutationResult>> ExecuteAsync(MyListMovieCommand command, CancellationToken cancellationToken = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(command.UserAccountId, command.ProfileId, cancellationToken);
        if (profile is null || profile.IsDeleted) return Result<MyListMutationResult>.Failure(MyListErrors.ProfileNotFound);
        // Removing a saved association remains possible even when the movie was soft-deleted.
        var item = await items.GetAsync(command.ProfileId, command.MovieId, cancellationToken);
        if (item is null) return Result<MyListMutationResult>.Success(new());
        items.Remove(item);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (PersistenceConcurrencyException)
        {
            // This use case only deletes one MyListItem (no concurrency properties).
            // A missing row means another DELETE already removed it.
        }
        return Result<MyListMutationResult>.Success(new());
    }
}
