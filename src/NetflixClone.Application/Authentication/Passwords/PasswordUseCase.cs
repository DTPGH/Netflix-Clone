using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Authentication.Devices;
namespace NetflixClone.Application.Authentication.Passwords;
public interface IPasswordResetSender
{
    Task SendAsync(int accountId, string email, string token, CancellationToken ct);
}
public interface IPasswordUseCase
{
    Task<Result<bool>> ForgotAsync(string email, CancellationToken ct);
    Task<Result<bool>> ResetAsync(int accountId, string token, string password, CancellationToken ct);
    Task<Result<bool>> ChangeAsync(int accountId, string oldPassword, string password, CancellationToken ct);
}
public sealed class PasswordUseCase(IUserAccountRepository accounts, IEmailConfirmationTokenService tokens,
    IPasswordResetSender sender, IPasswordHasher hasher, IClock clock, IUnitOfWork unitOfWork,
    IAuthenticationMutationScopeFactory scopes, IDeviceRepository devices, IRefreshTokenRepository refreshTokens) : IPasswordUseCase
{
    private static readonly Error Invalid = new("Auth.Password.InvalidReset", "The reset link is invalid or expired.", ErrorType.Validation);
    private static readonly Error Conflict = new("Auth.Password.ConcurrentChange", "Password data changed. Please try again.", ErrorType.Conflict);
    private static bool Valid(string password) => password.Length is >= 8 and <= 64;
    public async Task<Result<bool>> ForgotAsync(string email, CancellationToken ct)
    {
        var normalized = email.Trim().ToLowerInvariant();
        await using var scope = await scopes.BeginByEmailAsync(normalized, ct);
        var account = scope is null ? null : await accounts.GetByEmailAsync(normalized, ct);
        if (account is null || account.IsLocked || !account.EmailConfirmed) return Result<bool>.Success(true);
        // The stored expiry also bounds resend frequency without adding schema.
        var now = clock.UtcNow;
        if (account.PasswordResetTokenExpiresAt > now.AddMinutes(29)) return Result<bool>.Success(true);
        var generated = tokens.Generate();
        account.PasswordResetTokenHash = generated.TokenHash;
        account.PasswordResetTokenExpiresAt = now.AddMinutes(30);
        try { await unitOfWork.SaveChangesAsync(ct); await scope!.CommitAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<bool>.Success(true); }
        await sender.SendAsync(account.Id, account.Email, generated.RawToken, ct);
        return Result<bool>.Success(true);
    }
    public async Task<Result<bool>> ResetAsync(int accountId, string token, string password, CancellationToken ct)
    {
        await using var scope = await scopes.BeginAsync(accountId, ct);
        var account = scope is null ? null : await accounts.GetByIdAsync(accountId, ct);
        if (account is null || account.IsLocked || !account.EmailConfirmed || token.Length != 64 || !token.All(Uri.IsHexDigit) ||
            account.PasswordResetTokenHash is null || account.PasswordResetTokenExpiresAt <= clock.UtcNow || account.PasswordResetTokenExpiresAt is null ||
            !tokens.Verify(token, account.PasswordResetTokenHash)) return Result<bool>.Failure(Invalid);
        if (!Valid(password)) return Result<bool>.Failure(new("Auth.Password.InvalidPassword", "Use 8–64 characters.", ErrorType.Validation));
        account.PasswordHash = hasher.Hash(password);
        account.PasswordResetTokenHash = null; account.PasswordResetTokenExpiresAt = null;
        account.FailedLoginCount = 0; account.LockoutEnd = null; account.UpdatedAt = clock.UtcNow;
        await AccountSessionRevocation.StageAsync(accountId, clock.UtcNow, devices, refreshTokens, ct);
        try { await unitOfWork.SaveChangesAsync(ct); await scope!.CommitAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<bool>.Failure(Invalid); }
        return Result<bool>.Success(true);
    }
    public async Task<Result<bool>> ChangeAsync(int accountId, string oldPassword, string password, CancellationToken ct)
    {
        await using var scope = await scopes.BeginAsync(accountId, ct);
        var account = scope is null ? null : await accounts.GetByIdAsync(accountId, ct);
        if (account is null || account.IsLocked || !account.EmailConfirmed)
            return Result<bool>.Failure(new("Auth.Password.Forbidden", "An eligible account is required.", ErrorType.Forbidden));
        if (!hasher.Verify(oldPassword, account.PasswordHash)) return Result<bool>.Failure(new("Auth.Password.WrongPassword", "The current password is incorrect.", ErrorType.Validation));
        if (!Valid(password)) return Result<bool>.Failure(new("Auth.Password.InvalidPassword", "Use 8–64 characters.", ErrorType.Validation));
        if (hasher.Verify(password, account.PasswordHash)) return Result<bool>.Failure(new("Auth.Password.SamePassword", "Choose a different password.", ErrorType.Validation));
        account.PasswordHash = hasher.Hash(password); account.UpdatedAt = clock.UtcNow;
        account.PasswordResetTokenHash = null; account.PasswordResetTokenExpiresAt = null;
        await AccountSessionRevocation.StageAsync(accountId, clock.UtcNow, devices, refreshTokens, ct);
        try { await unitOfWork.SaveChangesAsync(ct); await scope!.CommitAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<bool>.Failure(Conflict); }
        return Result<bool>.Success(true);
    }
}
