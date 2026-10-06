using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Profiles;

public sealed class ProfileAccessGuard(IProfileRepository profiles, IUserAccountRepository accounts,
    IProfileUnlockTokenService tokens) : IProfileAccessGuard
{
    public async Task<Result<bool>> CheckAsync(int accountId, int profileId, string? unlockToken, CancellationToken ct)
    {
        var profile = await profiles.GetByIdForAccountAsync(accountId, profileId, ct);
        if (profile is null || profile.IsDeleted) return Result<bool>.Failure(ProfileErrors.NotFound);
        var account = await accounts.GetByIdAsync(accountId, ct);
        if (account is null || account.IsLocked || !account.EmailConfirmed)
            return Result<bool>.Failure(ProfileErrors.AccountNotFound);
        if (profile.PinHash is not null && (string.IsNullOrEmpty(unlockToken) ||
            !tokens.Verify(unlockToken, accountId, profileId, profile.PinHash, account.PasswordHash)))
            return Result<bool>.Failure(ProfileErrors.UnlockRequired);
        return Result<bool>.Success(true);
    }
}
