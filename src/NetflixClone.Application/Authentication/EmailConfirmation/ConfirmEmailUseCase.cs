using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.EmailConfirmation;

public sealed class ConfirmEmailUseCase : IConfirmEmailUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IEmailConfirmationTokenService _emailConfirmationTokenService;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmEmailUseCase(
        IUserAccountRepository userAccountRepository,
        IEmailConfirmationTokenService emailConfirmationTokenService,
        IClock clock,
        IUnitOfWork unitOfWork)
    {
        _userAccountRepository = userAccountRepository;
        _emailConfirmationTokenService = emailConfirmationTokenService;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ConfirmEmailResult>> ExecuteAsync(ConfirmEmailCommand command, CancellationToken cancellationToken = default)
    {
        var account = await _userAccountRepository.GetByIdAsync(command.UserAccountId, cancellationToken);

        if (account is null)
        {
            return Result<ConfirmEmailResult>.Failure(ConfirmEmailErrors.InvalidOrExpiredToken);
        }

        // Idempotent behavior:
        // click confirmation link lần hai vẫn coi là thành công.
        if (account.EmailConfirmed)
        {
            return Result<ConfirmEmailResult>.Success(
                new ConfirmEmailResult(
                    account.Id,
                    account.Email,
                    true));
        }

        if (account.EmailConfirmationTokenHash is null || account.EmailConfirmationTokenExpiresAt is null)
        {
            return Result<ConfirmEmailResult>.Failure(ConfirmEmailErrors.InvalidOrExpiredToken);
        }

        if (account.EmailConfirmationTokenExpiresAt <= _clock.UtcNow)
        {
            return Result<ConfirmEmailResult>.Failure(ConfirmEmailErrors.InvalidOrExpiredToken);
        }

        var tokenIsValid =
            _emailConfirmationTokenService.Verify(
                command.Token,
                account.EmailConfirmationTokenHash);

        if (!tokenIsValid)
        {
            return Result<ConfirmEmailResult>.Failure(ConfirmEmailErrors.InvalidOrExpiredToken);
        }

        account.EmailConfirmed = true;
        account.EmailConfirmedAt = _clock.UtcNow;

        account.EmailConfirmationTokenHash = null;
        account.EmailConfirmationTokenExpiresAt = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<ConfirmEmailResult>.Success(
            new ConfirmEmailResult(
                account.Id,
                account.Email,
                true));
    }
}