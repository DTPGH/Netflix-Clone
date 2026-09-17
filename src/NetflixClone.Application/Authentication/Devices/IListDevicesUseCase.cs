using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Authentication.Devices;
public interface IListDevicesUseCase
{
    Task<Result<ListDevicesResult>> ExecuteAsync(ListDevicesQuery query, CancellationToken cancellationToken = default);
}
