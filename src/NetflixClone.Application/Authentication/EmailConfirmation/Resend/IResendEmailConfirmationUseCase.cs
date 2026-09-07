using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.EmailConfirmation.Resend;

public interface IResendEmailConfirmationUseCase
{
    Task<Result<ResendEmailConfirmationResult>> ExecuteAsync(ResendEmailConfirmationCommand command, CancellationToken cancellationToken = default);
}