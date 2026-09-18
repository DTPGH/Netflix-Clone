using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Profiles;
public interface ICreateProfileUseCase
{
    Task<Result<CreateProfileResult>> ExecuteAsync(CreateProfileCommand command, CancellationToken cancellationToken = default);
}
