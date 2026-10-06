using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Authentication.Devices;

internal static class AccountSessionRevocation
{
    // Stage only; caller owns the account lock and the single atomic commit.
    public static async Task StageAsync(int accountId, DateTime now, IDeviceRepository devices, IRefreshTokenRepository tokens, CancellationToken ct)
    {
        foreach (var device in await devices.GetUnrevokedByAccountAsync(accountId, ct))
            device.RevokedAt = now < device.FirstSeenAt ? device.FirstSeenAt : now;
        foreach (var token in await tokens.GetUnrevokedByAccountAsync(accountId, ct)) token.RevokedAt = now;
    }
}
public interface IRevokeAllDevicesUseCase
{
    Task<Result<bool>> ExecuteAsync(int accountId, CancellationToken ct);
}
public sealed class RevokeAllDevicesUseCase(IAuthenticationMutationScopeFactory scopes, IUserAccountRepository accounts,
    IDeviceRepository devices, IRefreshTokenRepository tokens, IClock clock, IUnitOfWork unitOfWork) : IRevokeAllDevicesUseCase
{
    public async Task<Result<bool>> ExecuteAsync(int accountId, CancellationToken ct)
    {
        await using var scope = await scopes.BeginAsync(accountId, ct);
        var account = scope is null ? null : await accounts.GetByIdAsync(accountId, ct);
        if (account is null || account.IsLocked || !account.EmailConfirmed)
            return Result<bool>.Failure(new("Auth.Devices.Forbidden", "An eligible account is required.", ErrorType.Forbidden));
        await AccountSessionRevocation.StageAsync(accountId, clock.UtcNow, devices, tokens, ct);
        try { await unitOfWork.SaveChangesAsync(ct); await scope!.CommitAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<bool>.Failure(DeviceErrors.ConcurrentChange); }
        return Result<bool>.Success(true);
    }
}
