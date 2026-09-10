using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;

public interface IDeviceRepository
{
    Task<Device?> GetByIdentifierHashAsync(int userAccountId, string identifierHash, CancellationToken cancellationToken = default);
    Task AddAsync(Device device, CancellationToken cancellationToken = default);
}
