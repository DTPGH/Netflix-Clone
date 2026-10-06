namespace NetflixClone.Web.Client.Models;
public sealed record DashboardPeriod(DateOnly From, DateOnly Through);
public sealed record DashboardCards(long NewAccounts, long EffectiveAccounts, decimal SimulatedRevenue, long QualifiedViews);
public sealed record DashboardDay(DateOnly Date, decimal SimulatedRevenue, long QualifiedViews);
public sealed record DashboardMovie(int MovieId, string Title, bool IsDeleted, long QualifiedViews, decimal AverageWatchedSeconds, decimal FinishedPercent);
public sealed record AdminDashboardReply(DashboardPeriod Period, DateTime GeneratedAtUtc, string TimeZone, string Currency,
    DashboardCards Cards, DashboardDay[] Days, DashboardMovie[] TopMovies);
