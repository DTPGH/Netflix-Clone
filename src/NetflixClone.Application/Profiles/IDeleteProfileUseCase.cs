using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Profiles;
public interface IDeleteProfileUseCase
{
    Task<Result<DeleteProfileResult>> ExecuteAsync(DeleteProfileCommand command, CancellationToken cancellationToken = default);
}
