using NetflixClone.Application.Admin.Users;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IAdminUserRepository
{
    Task<AdminUserPage> ListAsync(UserSearch search, CancellationToken ct);
    Task<UserAccount?> GetAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Role>> RolesAsync(CancellationToken ct);
    Task<int> CountEligibleAdminsAsync(CancellationToken ct);
    void RemoveRole(UserRole role);
    void AddLog(AdminActionLog log);
    Task<AdminLogPage> LogsAsync(LogSearch search, CancellationToken ct);
}
public interface IAdminUserMutationScopeFactory
{
    Task<IAdminUserMutationScope> BeginAsync(int targetId, CancellationToken ct);
}
public interface IAdminUserMutationScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
