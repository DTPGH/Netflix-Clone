namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IProfileCreationScopeFactory
{
    // Null means the account does not exist. No EF/SQL types cross this boundary.
    Task<IProfileCreationScope?> BeginAsync(int userAccountId, CancellationToken cancellationToken = default);
}
