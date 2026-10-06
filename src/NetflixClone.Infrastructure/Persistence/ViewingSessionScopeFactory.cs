using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetflixClone.Application.Common.Abstractions.Persistence;
namespace NetflixClone.Infrastructure.Persistence;
public sealed class ViewingSessionScopeFactory(NetflixCloneDbContext db) : IViewingSessionScopeFactory
{
    public async Task<IViewingSessionScope?> BeginAsync(int accountId, string deviceHash, CancellationToken ct)
    {
        var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Keep the same account -> device lock order as auth issuance/global revocation.
            var accountIds = await db.Database.SqlQuery<int>($"SELECT Id AS [Value] FROM dbo.UserAccounts WITH (UPDLOCK,HOLDLOCK) WHERE Id = {accountId}").ToListAsync(ct);
            if (accountIds.Count == 0) { await transaction.DisposeAsync(); return null; }
            // Serialize short checkpoint/start transactions for this device across API instances.
            // Also prevents a device being revoked between eligibility check and commit.
            var ids = await db.Database.SqlQuery<int>($"SELECT Id AS [Value] FROM dbo.Devices WITH (UPDLOCK,HOLDLOCK) WHERE UserAccountId = {accountId} AND DeviceIdentifierHash = {deviceHash} AND RevokedAt IS NULL").ToListAsync(ct);
            if (ids.Count == 0) { await transaction.DisposeAsync(); return null; }
            return new Scope(ids.Single(), transaction);
        }
        catch { await transaction.DisposeAsync(); throw; }
    }
    private sealed class Scope(int deviceId, IDbContextTransaction transaction) : IViewingSessionScope
    {
        public int DeviceId => deviceId;
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
