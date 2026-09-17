using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Authentication.Devices;
public sealed class ListDevicesUseCase(IDeviceRepository devices) : IListDevicesUseCase
{
    public async Task<Result<ListDevicesResult>> ExecuteAsync(ListDevicesQuery query, CancellationToken cancellationToken = default)
    {
        var items = await devices.ListByUserAccountIdAsync(query.UserAccountId, cancellationToken);
        return Result<ListDevicesResult>.Success(new(items));
    }
}
