using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Profiles;
public interface IUpdateProfileUseCase
{
    Task<Result<UpdateProfileResult>> ExecuteAsync(UpdateProfileCommand command, CancellationToken cancellationToken = default);
}
