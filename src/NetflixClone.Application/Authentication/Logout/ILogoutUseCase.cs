using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.Logout;

public interface ILogoutUseCase
{
    Task<Result<LogoutResult>> ExecuteAsync(LogoutCommand command, CancellationToken cancellationToken = default);
}
