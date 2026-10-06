namespace NetflixClone.Application.Common.Abstractions.Persistence;

// Account-wide serialization for issuance and revocation across API instances.
public interface IAuthenticationMutationScopeFactory
{
    Task<IAuthenticationMutationScope?> BeginAsync(int accountId, CancellationToken ct);
    Task<IAuthenticationMutationScope?> BeginByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<IAuthenticationMutationScope?> BeginByRefreshTokenHashAsync(string tokenHash, CancellationToken ct);
}
public interface IAuthenticationMutationScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
