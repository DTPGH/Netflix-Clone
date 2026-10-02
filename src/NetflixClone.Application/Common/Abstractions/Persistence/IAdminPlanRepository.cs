using NetflixClone.Application.Admin.Plans;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IAdminPlanRepository
{
    Task<IReadOnlyList<AdminPlan>> ListAsync(CancellationToken ct);
    Task<Plan?> GetAsync(int id, CancellationToken ct);
    Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct);
    Task<int> SubscriptionCountAsync(int id, CancellationToken ct);
    Task AddAsync(Plan plan, CancellationToken ct);
}
public interface IAdminPlanMutationScopeFactory { Task<IAdminPlanMutationScope> BeginAsync(CancellationToken ct); }
public interface IAdminPlanMutationScope : IAsyncDisposable { Task CommitAsync(CancellationToken ct); }
