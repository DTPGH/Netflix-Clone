using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.EmailConfirmation;

public interface IConfirmEmailUseCase
{
    Task<Result<ConfirmEmailResult>> ExecuteAsync(ConfirmEmailCommand command, CancellationToken cancellationToken = default);
}