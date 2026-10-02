using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Admin.Plans;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Repositories;
public sealed class AdminPlanRepository(NetflixCloneDbContext db) : IAdminPlanRepository
{
    public async Task<IReadOnlyList<AdminPlan>> ListAsync(CancellationToken ct)
    {
        var items = await db.Plans.AsNoTracking().OrderBy(p => p.Price).ThenBy(p => p.Id)
            .Select(p => new AdminPlan(p.Id, p.Name, p.Price, p.MaxConcurrentStreams, p.MaxQuality, p.IsActive,
                p.Subscriptions.Count, p.CreatedAt, p.UpdatedAt)).ToListAsync(ct);
        return items.Select(p => p with { CreatedAtUtc = DateTime.SpecifyKind(p.CreatedAtUtc, DateTimeKind.Utc),
            UpdatedAtUtc = DateTime.SpecifyKind(p.UpdatedAtUtc, DateTimeKind.Utc) }).ToArray();
    }
    public Task<Plan?> GetAsync(int id, CancellationToken ct) => db.Plans.SingleOrDefaultAsync(p => p.Id == id, ct);
    public Task<bool> NameExistsAsync(string name, int? exceptId, CancellationToken ct)
        => db.Plans.AnyAsync(p => p.Name == name && (!exceptId.HasValue || p.Id != exceptId.Value), ct);
    public Task<int> SubscriptionCountAsync(int id, CancellationToken ct) => db.Subscriptions.CountAsync(s => s.PlanId == id, ct);
    public async Task AddAsync(Plan plan, CancellationToken ct) => await db.Plans.AddAsync(plan, ct);
}
