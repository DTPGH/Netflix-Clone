using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetflixClone.Application.Common.Abstractions.Persistence;

namespace NetflixClone.Infrastructure.Persistence;

public sealed class ProfileCreationScopeFactory(NetflixCloneDbContext dbContext) : IProfileCreationScopeFactory
{
    public async Task<IProfileCreationScope?> BeginAsync(int userAccountId, CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            // Execute a real, parameterized database read, not a tracked entity lookup.
            // The update lock survives until commit/disposal, across count and insertion.
            var accounts = await dbContext.Database.SqlQuery<int>(
                $@"SELECT Id AS [Value] FROM dbo.UserAccounts WITH (UPDLOCK, HOLDLOCK)
                   WHERE Id = {userAccountId}").ToListAsync(cancellationToken);
            if (accounts.Count == 0)
            {
                await transaction.DisposeAsync();
                return null;
            }
            return new Scope(transaction);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw; // Lock timeouts/deadlocks are infrastructure failures, never the profile limit.
        }
    }

    private sealed class Scope(IDbContextTransaction transaction) : IProfileCreationScope
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);
        // Disposing an uncommitted transaction rolls it back and releases the account lock.
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
