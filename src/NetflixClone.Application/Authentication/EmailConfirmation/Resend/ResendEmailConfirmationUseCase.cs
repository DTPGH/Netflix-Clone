using NetflixClone.Application.Common.Abstractions.Messaging;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.EmailConfirmation.Resend;

public sealed class ResendEmailConfirmationUseCase : IResendEmailConfirmationUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IEmailConfirmationTokenService _emailConfirmationTokenService;
    private readonly IEmailConfirmationSender _emailConfirmationSender;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;

    public ResendEmailConfirmationUseCase(
        IUserAccountRepository userAccountRepository,
        IEmailConfirmationTokenService emailConfirmationTokenService,
        IEmailConfirmationSender emailConfirmationSender,
        IClock clock,
        IUnitOfWork unitOfWork
    )
    {
        _userAccountRepository = userAccountRepository;
        _emailConfirmationTokenService = emailConfirmationTokenService;
        _emailConfirmationSender = emailConfirmationSender;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ResendEmailConfirmationResult>> ExecuteAsync(ResendEmailConfirmationCommand command, CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();

        var account = await _userAccountRepository.GetByEmailAsync(email, cancellationToken);

        if (account is null || account.EmailConfirmed)
        {
            return Result<ResendEmailConfirmationResult>.Success(new());
        }

        var utcNow = _clock.UtcNow;

        if (account.EmailConfirmationLastSentAt.HasValue &&
            utcNow - account.EmailConfirmationLastSentAt.Value < EmailConfirmationRules.ResendCooldown)
        {
            return Result<ResendEmailConfirmationResult>.Success(new());
        }

        var token = _emailConfirmationTokenService.Generate();

        account.EmailConfirmationTokenHash = token.TokenHash;
        account.EmailConfirmationTokenExpiresAt = utcNow.Add(EmailConfirmationRules.TokenLifetime);
        account.EmailConfirmationLastSentAt = utcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _emailConfirmationSender.SendAsync(account.Id, account.Email, token.RawToken, cancellationToken);

        return Result<ResendEmailConfirmationResult>.Success(new());
    }
}