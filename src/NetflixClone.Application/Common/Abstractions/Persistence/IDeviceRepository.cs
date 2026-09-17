using NetflixClone.Domain.Entities;
using NetflixClone.Application.Authentication.Devices;
namespace NetflixClone.Application.Common.Abstractions.Persistence;

public interface IDeviceRepository
{
    Task<IReadOnlyList<DeviceSummary>> ListByUserAccountIdAsync(int userAccountId, CancellationToken cancellationToken = default);
    Task<Device?> GetByIdForAccountAsync(int userAccountId, int deviceId, CancellationToken cancellationToken = default);
    Task<Device?> GetByIdentifierHashAsync(int userAccountId, string identifierHash, CancellationToken cancellationToken = default);
    Task AddAsync(Device device, CancellationToken cancellationToken = default);
}
