using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Infrastructure.Persistence.Repositories;

public sealed class DeviceRepository : IDeviceRepository
{
    private readonly NetflixCloneDbContext _dbContext;

    public DeviceRepository(NetflixCloneDbContext dbContext) => _dbContext = dbContext;

    public Task<Device?> GetByIdentifierHashAsync(int userAccountId, string identifierHash, CancellationToken cancellationToken = default)
    {
        return _dbContext.Devices.FirstOrDefaultAsync(
            device => device.UserAccountId == userAccountId && device.DeviceIdentifierHash == identifierHash,
            cancellationToken);
    }

    public async Task AddAsync(Device device, CancellationToken cancellationToken = default)
    {
        await _dbContext.Devices.AddAsync(device, cancellationToken);
    }
}
