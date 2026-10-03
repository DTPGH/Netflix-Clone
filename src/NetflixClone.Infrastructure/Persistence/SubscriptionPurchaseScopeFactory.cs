using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetflixClone.Application.Common.Abstractions.Persistence;
namespace NetflixClone.Infrastructure.Persistence;
public sealed class SubscriptionPurchaseScopeFactory(NetflixCloneDbContext db) : ISubscriptionPurchaseScopeFactory
{
    public async Task<ISubscriptionPurchaseScope?> BeginAsync(int accountId, CancellationToken ct)
    {
        var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var ids = await db.Database.SqlQuery<int>($"SELECT Id AS [Value] FROM dbo.UserAccounts WITH (UPDLOCK,HOLDLOCK) WHERE Id = {accountId}").ToListAsync(ct);
            if (ids.Count == 0) { await transaction.DisposeAsync(); return null; }
            return new Scope(transaction);
        }
        catch { await transaction.DisposeAsync(); throw; }
    }
    private sealed class Scope(IDbContextTransaction transaction) : ISubscriptionPurchaseScope
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
