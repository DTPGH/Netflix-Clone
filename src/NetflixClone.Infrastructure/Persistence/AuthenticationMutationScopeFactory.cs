using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetflixClone.Application.Common.Abstractions.Persistence;
namespace NetflixClone.Infrastructure.Persistence;

public sealed class AuthenticationMutationScopeFactory(NetflixCloneDbContext db) : IAuthenticationMutationScopeFactory
{
    public Task<IAuthenticationMutationScope?> BeginAsync(int accountId, CancellationToken ct) => LockAsync(accountId, null, ct);
    public Task<IAuthenticationMutationScope?> BeginByEmailAsync(string email, CancellationToken ct) => LockAsync(null, email, ct);
    public async Task<IAuthenticationMutationScope?> BeginByRefreshTokenHashAsync(string hash, CancellationToken ct)
    {
        // Scalar lookup only: no tracked token/account/device may be read before the account lock.
        var ids = await db.Database.SqlQuery<int>($"SELECT UserAccountId AS [Value] FROM dbo.RefreshTokens WHERE TokenHash = {hash}").ToListAsync(ct);
        return ids.Count == 0 ? null : await BeginAsync(ids.Single(), ct);
    }
    private async Task<IAuthenticationMutationScope?> LockAsync(int? id, string? email, CancellationToken ct)
    {
        var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var ids = id.HasValue
                ? await db.Database.SqlQuery<int>($"SELECT Id AS [Value] FROM dbo.UserAccounts WITH (UPDLOCK,HOLDLOCK) WHERE Id = {id.Value}").ToListAsync(ct)
                : await db.Database.SqlQuery<int>($"SELECT Id AS [Value] FROM dbo.UserAccounts WITH (UPDLOCK,HOLDLOCK) WHERE Email = {email}").ToListAsync(ct);
            if (ids.Count == 0) { await transaction.DisposeAsync(); return null; }
            return new Scope(transaction);
        }
        catch { await transaction.DisposeAsync(); throw; }
    }
    private sealed class Scope(IDbContextTransaction transaction) : IAuthenticationMutationScope
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
