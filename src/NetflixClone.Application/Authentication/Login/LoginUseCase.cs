using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.Login;

public sealed class LoginUseCase : ILoginUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IClock _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessTokenGenerator _accessTokenGenerator;

    public LoginUseCase(
        IUserAccountRepository userAccountRepository,
        IPasswordHasher passwordHasher,
        IClock clock,
        IUnitOfWork unitOfWork,
        IAccessTokenGenerator accessTokenGenerator)
    {
        _userAccountRepository = userAccountRepository;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _accessTokenGenerator = accessTokenGenerator;
    }

    public async Task<Result<LoginResult>> ExecuteAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var account = await _userAccountRepository.GetByEmailAsync(email, cancellationToken);

        if (account is null)
        {
            return Result<LoginResult>.Failure(LoginErrors.InvalidCredentials);
        }

        var utcNow = _clock.UtcNow;

        if (account.LockoutEnd > utcNow)
        {
            return Result<LoginResult>.Failure(LoginErrors.TemporarilyLocked);
        }

        var failedLoginStateChanged = false;

        if (account.LockoutEnd.HasValue)
        {
            account.FailedLoginCount = 0;
            account.LockoutEnd = null;
            failedLoginStateChanged = true;
        }

        if (!_passwordHasher.Verify(command.Password, account.PasswordHash))
        {
            account.FailedLoginCount++;

            if (account.FailedLoginCount >= LoginRules.MaxFailedAttempts)
            {
                account.LockoutEnd = utcNow.Add(LoginRules.LockoutDuration);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResult>.Failure(account.LockoutEnd.HasValue
                ? LoginErrors.TemporarilyLocked
                : LoginErrors.InvalidCredentials);
        }

        if (account.FailedLoginCount != 0)
        {
            account.FailedLoginCount = 0;
            failedLoginStateChanged = true;
        }

        if (failedLoginStateChanged)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        if (account.IsLocked)
        {
            return Result<LoginResult>.Failure(LoginErrors.AccountLocked);
        }

        if (!account.EmailConfirmed)
        {
            return Result<LoginResult>.Failure(LoginErrors.EmailNotConfirmed);
        }

        var roles = await _userAccountRepository.GetRoleNamesAsync(account.Id, cancellationToken);
        if (roles.Count == 0)
        {
            return Result<LoginResult>.Failure(LoginErrors.RolesNotConfigured);
        }

        var token = _accessTokenGenerator.Generate(account.Id, roles);
        return Result<LoginResult>.Success(new LoginResult(
            account.Id, account.Email, token.AccessToken, token.ExpiresAtUtc));
    }
}
