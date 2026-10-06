using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;

namespace NetflixClone.Application.Profiles;

public sealed class ProfilePinUseCase(IProfileRepository profiles, IUserAccountRepository accounts,
    IAuthenticationMutationScopeFactory scopes, ProfileAccountPasswordVerifier passwords,
    IPasswordHasher hasher, IProfileUnlockTokenService tokens, IClock clock, IUnitOfWork unitOfWork) : IProfilePinUseCase
{
    public async Task<Result<ProfileSummary>> SetAsync(int accountId, int profileId, string accountPassword, string? pin, CancellationToken ct)
    {
        if (pin is not null && !ProfilePinRules.IsValid(pin)) return Result<ProfileSummary>.Failure(ProfileErrors.InvalidPin);
        await using var scope = await scopes.BeginAsync(accountId, ct);
        if (scope is null) return Result<ProfileSummary>.Failure(ProfileErrors.AccountNotFound);
        var profile = await profiles.GetByIdForAccountAsync(accountId, profileId, ct);
        if (profile is null || profile.IsDeleted) return Result<ProfileSummary>.Failure(ProfileErrors.NotFound);
        var password = await passwords.VerifyAsync(accountId, accountPassword, ct);
        if (password.IsFailure) return Result<ProfileSummary>.Failure(password.Error!);
        profile.PinHash = pin is null ? null : hasher.Hash(pin);
        profile.PinFailedAttempts = 0;
        profile.PinLockoutEnd = null;
        profile.UpdatedAt = ProfileRules.NextUpdatedAt(profile.UpdatedAt, clock.UtcNow);
        try { await unitOfWork.SaveChangesAsync(ct); await scope.CommitAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<ProfileSummary>.Failure(ProfileErrors.ConcurrentChange); }
        return Result<ProfileSummary>.Success(ProfileSummary.From(profile));
    }

    public async Task<Result<GeneratedProfileUnlockToken>> UnlockAsync(int accountId, int profileId, string? pin, CancellationToken ct)
    {
        if (pin is not null && !ProfilePinRules.IsValid(pin))
            return Result<GeneratedProfileUnlockToken>.Failure(ProfileErrors.InvalidPin);
        // SQL account-row lock makes the read/check/increment atomic across API instances.
        await using var scope = await scopes.BeginAsync(accountId, ct);
        var account = scope is null ? null : await accounts.GetByIdAsync(accountId, ct);
        if (account is null || account.IsLocked || !account.EmailConfirmed)
            return Result<GeneratedProfileUnlockToken>.Failure(ProfileErrors.AccountNotFound);
        var profile = await profiles.GetByIdForAccountAsync(accountId, profileId, ct);
        if (profile is null || profile.IsDeleted) return Result<GeneratedProfileUnlockToken>.Failure(ProfileErrors.NotFound);
        var now = clock.UtcNow;
        if (profile.PinLockoutEnd > now)
            return Result<GeneratedProfileUnlockToken>.Failure(ProfileErrors.PinTemporarilyLocked);
        if (profile.PinLockoutEnd is not null) { profile.PinFailedAttempts = 0; profile.PinLockoutEnd = null; }
        var valid = profile.PinHash is null || (pin is not null && hasher.Verify(pin, profile.PinHash));
        if (!valid)
        {
            profile.PinFailedAttempts++;
            if (profile.PinFailedAttempts >= ProfilePinRules.MaxFailedAttempts)
                profile.PinLockoutEnd = now.Add(ProfilePinRules.LockoutDuration);
        }
        else { profile.PinFailedAttempts = 0; profile.PinLockoutEnd = null; }
        try { await unitOfWork.SaveChangesAsync(ct); await scope!.CommitAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<GeneratedProfileUnlockToken>.Failure(ProfileErrors.ConcurrentChange); }
        if (!valid) return Result<GeneratedProfileUnlockToken>.Failure(
            profile.PinLockoutEnd > now ? ProfileErrors.PinTemporarilyLocked : ProfileErrors.WrongPin);
        // Issue only after the attempt/reset commit succeeds. No token enters database storage.
        return Result<GeneratedProfileUnlockToken>.Success(tokens.Generate(accountId, profileId, profile.PinHash, account.PasswordHash));
    }
}
