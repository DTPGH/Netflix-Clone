using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
namespace NetflixClone.Application.Admin.Dashboard;

public sealed record DashboardPeriod(DateOnly From, DateOnly Through);
public sealed record DashboardCards(long NewAccounts, long EffectiveAccounts, decimal SimulatedRevenue, long QualifiedViews);
public sealed record DashboardDay(DateOnly Date, decimal SimulatedRevenue, long QualifiedViews);
public sealed record DashboardMovie(int MovieId, string Title, bool IsDeleted, long QualifiedViews, decimal AverageWatchedSeconds, decimal FinishedPercent);
public sealed record AdminDashboard(DashboardPeriod Period, DateTime GeneratedAtUtc, string TimeZone, string Currency,
    DashboardCards Cards, IReadOnlyList<DashboardDay> Days, IReadOnlyList<DashboardMovie> TopMovies);
public interface IAdminDashboardQueries
{
    Task<AdminDashboard> ReadAsync(DashboardPeriod period, DateTime now, CancellationToken ct);
}
public interface IAdminDashboardUseCase
{
    Task<Result<AdminDashboard>> GetAsync(int actorId, DashboardPeriod period, CancellationToken ct);
}
public sealed class AdminDashboardUseCase(IUserAccountRepository accounts, IAdminDashboardQueries queries, IClock clock) : IAdminDashboardUseCase
{
    public async Task<Result<AdminDashboard>> GetAsync(int actorId, DashboardPeriod period, CancellationToken ct)
    {
        var actor = await accounts.GetByIdAsync(actorId, ct);
        if (actor is null || actor.IsLocked || !actor.EmailConfirmed ||
            !(await accounts.GetRoleNamesAsync(actorId, ct)).Contains(RoleNames.Admin, StringComparer.Ordinal))
            return Result<AdminDashboard>.Failure(new("AdminDashboard.Forbidden", "Current administrator access is required.", ErrorType.Forbidden));
        var today = DateOnly.FromDateTime(clock.UtcNow.AddHours(7));
        if (period.From.Year < 2000 || period.Through < period.From || period.Through > today || period.Through.DayNumber - period.From.DayNumber >= 366)
            return Result<AdminDashboard>.Failure(new("AdminDashboard.InvalidRange", "Choose at most 366 days through today, using Vietnam dates.", ErrorType.Validation));
        return Result<AdminDashboard>.Success(await queries.ReadAsync(period, clock.UtcNow, ct));
    }
}
