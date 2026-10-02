using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetflixClone.Application.Common.Abstractions.Persistence;
namespace NetflixClone.Infrastructure.Persistence;
public sealed class AdminGenreMutationScopeFactory(NetflixCloneDbContext db) : IAdminGenreMutationScopeFactory
{
    public async Task<IAdminGenreMutationScope> BeginAsync(CancellationToken ct)
    {
        var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // Small MVP lookup table: serialize genre mutations even when no rows exist.
            // The table X lock also blocks new FK references until delete/commit finishes.
            // Keep this transaction short; public genre reads can wait while it is held.
            await db.Database.SqlQuery<int>(
                $"SELECT Id AS [Value] FROM dbo.Genres WITH (TABLOCKX, HOLDLOCK)").ToListAsync(ct);
            return new Scope(transaction);
        }
        catch { await transaction.DisposeAsync(); throw; }
    }
    private sealed class Scope(IDbContextTransaction transaction) : IAdminGenreMutationScope
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
