using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Authentication.Devices;
public sealed class RevokeDeviceUseCase(
    IDeviceRepository devices, IRefreshTokenRepository tokens, IClock clock, IUnitOfWork unitOfWork) : IRevokeDeviceUseCase
{
    public async Task<Result<RevokeDeviceResult>> ExecuteAsync(RevokeDeviceCommand command, CancellationToken cancellationToken = default)
    {
        var device = await devices.GetByIdForAccountAsync(command.UserAccountId, command.DeviceId, cancellationToken);
        if (device is null) return Result<RevokeDeviceResult>.Failure(DeviceErrors.NotFound);
        if (device.RevokedAt.HasValue) return Result<RevokeDeviceResult>.Success(new());

        var utcNow = clock.UtcNow;
        var activeTokens = await tokens.GetActiveByDeviceAsync(command.UserAccountId, command.DeviceId, utcNow, cancellationToken);
        device.RevokedAt = utcNow;
        foreach (var token in activeTokens) token.RevokedAt = utcNow;
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConcurrencyException)
        {
            // The entire save rolled back, including Device.RevokedAt. Never report success or retry this context.
            return Result<RevokeDeviceResult>.Failure(DeviceErrors.ConcurrentChange);
        }
        return Result<RevokeDeviceResult>.Success(new());
    }
}
