using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Authentication.Register;

public interface IRegisterAccountUseCase
{
    Task<Result<RegisterAccountResult>> ExecuteAsync(RegisterAccountCommand command, CancellationToken cancellationToken = default);
}