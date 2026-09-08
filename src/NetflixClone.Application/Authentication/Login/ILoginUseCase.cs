using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.Login;

public interface ILoginUseCase
{
    Task<Result<LoginResult>> ExecuteAsync(LoginCommand command, CancellationToken cancellationToken = default);
}
