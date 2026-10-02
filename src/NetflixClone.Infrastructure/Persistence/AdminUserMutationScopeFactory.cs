using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Constants;
namespace NetflixClone.Infrastructure.Persistence;
public sealed class AdminUserMutationScopeFactory(NetflixCloneDbContext db) : IAdminUserMutationScopeFactory
{
    public async Task<IAdminUserMutationScope> BeginAsync(int targetId, CancellationToken ct)
    {
        var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // All Admin account/role mutations acquire this same existing row first.
            // Serializes last-admin checks across API instances without in-memory locks.
            var gate = await db.Database.SqlQuery<int>(
                $"SELECT Id AS [Value] FROM dbo.Roles WITH (UPDLOCK, HOLDLOCK) WHERE Name = {RoleNames.Admin}").ToListAsync(ct);
            if (gate.Count != 1) throw new InvalidOperationException("The Admin role is not configured.");
            // Protect the target while reading/updating it; auth writes wait, preserving unrelated fields.
            await db.Database.SqlQuery<int>(
                $"SELECT Id AS [Value] FROM dbo.UserAccounts WITH (UPDLOCK, HOLDLOCK) WHERE Id = {targetId}").ToListAsync(ct);
            return new Scope(transaction);
        }
        catch { await transaction.DisposeAsync(); throw; }
    }
    private sealed class Scope(IDbContextTransaction transaction) : IAdminUserMutationScope
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
