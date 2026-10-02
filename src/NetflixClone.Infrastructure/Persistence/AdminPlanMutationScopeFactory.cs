using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetflixClone.Application.Common.Abstractions.Persistence;
namespace NetflixClone.Infrastructure.Persistence;
public sealed class AdminPlanMutationScopeFactory(NetflixCloneDbContext db) : IAdminPlanMutationScopeFactory
{
    public async Task<IAdminPlanMutationScope> BeginAsync(CancellationToken ct)
    {
        var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // Tiny plan catalog: serialize names/configuration changes and block new
            // subscription FK references until the checked configuration is committed.
            await db.Database.SqlQuery<int>($"SELECT Id AS [Value] FROM dbo.Plans WITH (TABLOCKX, HOLDLOCK)").ToListAsync(ct);
            return new Scope(transaction);
        }
        catch { await transaction.DisposeAsync(); throw; }
    }
    private sealed class Scope(IDbContextTransaction transaction) : IAdminPlanMutationScope
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
