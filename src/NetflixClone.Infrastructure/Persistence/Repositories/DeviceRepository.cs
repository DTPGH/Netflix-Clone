using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Authentication.Devices;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;

namespace NetflixClone.Infrastructure.Persistence.Repositories;

public sealed class DeviceRepository : IDeviceRepository
{
    private readonly NetflixCloneDbContext _dbContext;

    public DeviceRepository(NetflixCloneDbContext dbContext) => _dbContext = dbContext;

    public async Task<IReadOnlyList<DeviceSummary>> ListByUserAccountIdAsync(int userAccountId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Devices.AsNoTracking()
            .Where(device => device.UserAccountId == userAccountId)
            .OrderByDescending(device => device.LastActiveAt).ThenBy(device => device.Id)
            .Select(device => new DeviceSummary(device.Id, device.DeviceName, device.DeviceType,
                DateTime.SpecifyKind(device.FirstSeenAt, DateTimeKind.Utc),
                DateTime.SpecifyKind(device.LastActiveAt, DateTimeKind.Utc),
                device.RevokedAt.HasValue ? DateTime.SpecifyKind(device.RevokedAt.Value, DateTimeKind.Utc) : (DateTime?)null))
            .ToListAsync(cancellationToken);
    }

    public Task<Device?> GetByIdForAccountAsync(int userAccountId, int deviceId, CancellationToken cancellationToken = default)
        => _dbContext.Devices.FirstOrDefaultAsync(
            device => device.UserAccountId == userAccountId && device.Id == deviceId, cancellationToken);

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
