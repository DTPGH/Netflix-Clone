using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Subscriptions;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Repositories;
public sealed class SubscriptionRepository(NetflixCloneDbContext db) : ISubscriptionRepository
{
    public async Task<IReadOnlyList<AvailablePlan>> ListPlansAsync(CancellationToken ct) =>
        await db.Plans.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.Price).ThenBy(p => p.Id)
            .Select(p => new AvailablePlan(p.Id, p.Name, p.Price, "VND", 30, p.MaxConcurrentStreams, p.MaxQuality,
                DateTime.SpecifyKind(p.UpdatedAt, DateTimeKind.Utc))).ToListAsync(ct);
    // Shared serializable read lock: Admin Plan edits/deactivation cannot change the quoted configuration before commit.
    public async Task<Plan?> GetPlanForPurchaseAsync(int planId, CancellationToken ct)
    {
        var plan = await db.Plans.FromSqlInterpolated($"SELECT * FROM dbo.Plans WITH (HOLDLOCK) WHERE Id = {planId}").SingleOrDefaultAsync(ct);
        // An expired subscription may have loaded this Plan earlier. Refresh tracked values under the acquired lock.
        if (plan is not null) await db.Entry(plan).ReloadAsync(ct);
        return plan;
    }
    public Task<PaymentTransaction?> FindPaymentAsync(int accountId, string code, CancellationToken ct) => db.PaymentTransactions
        .Include(p => p.Subscription).ThenInclude(s => s.Plan)
        .SingleOrDefaultAsync(p => p.TransactionCode == code && p.Subscription.UserAccountId == accountId, ct);
    public async Task<IReadOnlyList<Subscription>> GetActiveRowsAsync(int accountId, CancellationToken ct) =>
        await db.Subscriptions.Include(s => s.Plan).Where(s => s.UserAccountId == accountId && s.Status == "Active").ToListAsync(ct);
    public Task<Subscription?> GetLatestAsync(int accountId, CancellationToken ct) => db.Subscriptions.AsNoTracking()
        .Include(s => s.Plan).Where(s => s.UserAccountId == accountId).OrderByDescending(s => s.StartDate).ThenByDescending(s => s.Id).FirstOrDefaultAsync(ct);
    public async Task<PaymentPage> ListPaymentsAsync(int accountId, int page, CancellationToken ct)
    {
        var query = db.PaymentTransactions.AsNoTracking().Where(p => p.Subscription.UserAccountId == accountId);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).Skip((page - 1) * 20).Take(20)
            .Select(p => new PaymentInfo(p.Id, p.SubscriptionId, p.Subscription.PlanId, p.Subscription.Plan.Name, p.Amount, "VND", p.Method,
                p.TransactionCode, p.Status, p.PaidAt.HasValue ? DateTime.SpecifyKind(p.PaidAt.Value, DateTimeKind.Utc) : null,
                DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc))).ToListAsync(ct);
        return new(items, count, page, 20);
    }
    public void Add(Subscription subscription, PaymentTransaction payment)
    {
        db.Subscriptions.Add(subscription);
        db.PaymentTransactions.Add(payment); // Relationship tracking supplies SubscriptionId during the same save.
    }
}
