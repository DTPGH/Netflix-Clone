using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NetflixClone.Application.Admin.Dashboard;
namespace NetflixClone.Infrastructure.Persistence.Queries;

public sealed class AdminDashboardQueries(NetflixCloneDbContext db, IMemoryCache cache) : IAdminDashboardQueries
{
    public async Task<AdminDashboard> ReadAsync(DashboardPeriod period, DateTime now, CancellationToken ct)
    {
        var key = ("admin-dashboard-v1", period);
        if (cache.TryGetValue<AdminDashboard>(key, out var cached)) return cached!;
        var from = period.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
        var to = period.Through.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddHours(-7);
        // Sequential queries: a scoped DbContext cannot run parallel operations.
        var accounts = await db.UserAccounts.AsNoTracking().LongCountAsync(a => a.CreatedAt >= from && a.CreatedAt < to, ct);
        var effective = await db.Subscriptions.AsNoTracking().Where(s => s.Status == "Active" && s.StartDate <= now && s.EndDate > now)
            .Select(s => s.UserAccountId).Distinct().LongCountAsync(ct);
        var payments = db.PaymentTransactions.AsNoTracking().Where(p => p.Status == "Succeeded" && p.Method == "Simulated" && p.PaidAt >= from && p.PaidAt < to);
        var revenue = await payments.SumAsync(p => (decimal?)p.Amount, ct) ?? 0;
        var sessions = db.ViewingSessions.AsNoTracking().Where(s => s.IsQualifiedView && s.StartedAt >= from && s.StartedAt < to);
        var views = await sessions.LongCountAsync(ct);
        var paymentDays = await payments.GroupBy(p => p.PaidAt!.Value.AddHours(7).Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(p => p.Amount) }).ToListAsync(ct);
        var viewDays = await sessions.GroupBy(s => s.StartedAt.AddHours(7).Date)
            .Select(g => new { Date = g.Key, Count = g.LongCount() }).ToListAsync(ct);
        var top = await TopRows(from, to).ToListAsync(ct);
        var money = paymentDays.ToDictionary(d => DateOnly.FromDateTime(d.Date), d => d.Amount);
        var counts = viewDays.ToDictionary(d => DateOnly.FromDateTime(d.Date), d => d.Count);
        var days = Enumerable.Range(0, period.Through.DayNumber - period.From.DayNumber + 1)
            .Select(i => period.From.AddDays(i)).Select(d => new DashboardDay(d, money.GetValueOrDefault(d), counts.GetValueOrDefault(d))).ToArray();
        var result = new AdminDashboard(period, now, "UTC+07:00", "VND", new(accounts, effective, revenue, views), days, top);
        cache.Set(key, result, TimeSpan.FromMinutes(2));
        return result;
    }
    private IQueryable<DashboardMovie> TopRows(DateTime from, DateTime to) =>
        db.ViewingSessions.AsNoTracking().Where(s => s.IsQualifiedView && s.StartedAt >= from && s.StartedAt < to)
            .GroupBy(s => new { s.MovieId, s.Movie.Title, s.Movie.IsDeleted })
            .OrderByDescending(g => g.LongCount()).ThenBy(g => g.Key.MovieId).Take(10)
            .Select(g => new DashboardMovie(g.Key.MovieId, g.Key.Title, g.Key.IsDeleted, g.LongCount(),
                g.Sum(s => (long)s.WatchedSeconds) / (decimal)g.LongCount(),
                100m * g.LongCount(s => s.EndReason == "Finished") / g.LongCount()));
}
