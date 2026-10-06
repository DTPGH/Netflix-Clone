using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Profiles;

// Called after acquiring the account mutation/creation scope; never commits itself.
public sealed class ProfileAccountPasswordVerifier(IUserAccountRepository accounts, IPasswordHasher hasher, IClock clock)
{
    public async Task<Result<bool>> VerifyAsync(int accountId, string? password, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(accountId, ct);
        if (account is null || account.IsLocked || !account.EmailConfirmed || account.LockoutEnd > clock.UtcNow)
            return Result<bool>.Failure(ProfileErrors.AccountNotFound);
        if (string.IsNullOrEmpty(password) || password.Length > 64 || !hasher.Verify(password, account.PasswordHash))
            return Result<bool>.Failure(ProfileErrors.WrongAccountPassword);
        return Result<bool>.Success(true);
    }
}
