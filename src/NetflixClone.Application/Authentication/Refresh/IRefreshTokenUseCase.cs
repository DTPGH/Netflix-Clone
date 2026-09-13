using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Authentication.Refresh;

public interface IRefreshTokenUseCase
{
    Task<Result<RefreshTokenResult>> ExecuteAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default);
}
