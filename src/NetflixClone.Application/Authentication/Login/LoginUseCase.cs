using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Authentication.Login;

public sealed class LoginUseCase : ILoginUseCase
{
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IPasswordHasher _passwordHasher;

    public LoginUseCase(IUserAccountRepository userAccountRepository, IPasswordHasher passwordHasher)
    {
        _userAccountRepository = userAccountRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<LoginResult>> ExecuteAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var account = await _userAccountRepository.GetByEmailAsync(email, cancellationToken);

        if (account is null || !_passwordHasher.Verify(command.Password, account.PasswordHash))
        {
            return Result<LoginResult>.Failure(LoginErrors.InvalidCredentials);
        }

        if (account.IsLocked)
        {
            return Result<LoginResult>.Failure(LoginErrors.AccountLocked);
        }

        if (!account.EmailConfirmed)
        {
            return Result<LoginResult>.Failure(LoginErrors.EmailNotConfirmed);
        }

        return Result<LoginResult>.Success(new LoginResult(account.Id, account.Email));
    }
}
