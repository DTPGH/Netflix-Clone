using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.MyList;
public interface IAddToMyListUseCase
{
    Task<Result<MyListMutationResult>> ExecuteAsync(MyListMovieCommand command, CancellationToken cancellationToken = default);
}
public sealed class AddToMyListUseCase(IProfileRepository profiles, IMyListRepository items,
    IUnitOfWork unitOfWork, IClock clock) : IAddToMyListUseCase
{
    public async Task<Result<MyListMutationResult>> ExecuteAsync(MyListMovieCommand command, CancellationToken cancellationToken = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(command.UserAccountId, command.ProfileId, cancellationToken);
        if (profile is null || profile.IsDeleted) return Result<MyListMutationResult>.Failure(MyListErrors.ProfileNotFound);
        if (!await items.MovieExistsAsync(command.MovieId, cancellationToken))
            return Result<MyListMutationResult>.Failure(MyListErrors.MovieNotFound);
        if (await items.GetAsync(command.ProfileId, command.MovieId, cancellationToken) is not null)
            return Result<MyListMutationResult>.Success(new());
        await items.AddAsync(new MyListItem { ProfileId = command.ProfileId, MovieId = command.MovieId, CreatedAt = clock.UtcNow }, cancellationToken);
        try { await unitOfWork.SaveChangesAsync(cancellationToken); }
        catch (MyListAlreadyExistsException)
        {
            // Another PUT inserted this exact pair. The failed save was rolled back;
            // do not retry or reuse its tracked state.
        }
        return Result<MyListMutationResult>.Success(new());
    }
}
