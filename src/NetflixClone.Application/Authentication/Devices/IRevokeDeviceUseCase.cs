using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Authentication.Devices;
public interface IRevokeDeviceUseCase
{
    Task<Result<RevokeDeviceResult>> ExecuteAsync(RevokeDeviceCommand command, CancellationToken cancellationToken = default);
}
