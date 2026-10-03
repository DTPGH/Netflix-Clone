namespace NetflixClone.Application.Admin.Reports;
public sealed record ViewingReportPeriod(DateTime FromUtc, DateTime ToUtc);
public sealed record MovieViewingReport(int MovieId, string Title, bool IsDeleted, long Sessions, long QualifiedViews,
    long UniqueProfiles, long WatchedSeconds, long FinishedSessions, long ActiveSessions, long TimedOutSessions);
public sealed record ViewingReportSummary(long Sessions, long QualifiedViews, long UniqueProfiles, long WatchedSeconds,
    long FinishedSessions, long ActiveSessions, long TimedOutSessions);
public sealed record ViewingReportData(IReadOnlyList<MovieViewingReport> Movies, long UniqueProfiles);
public sealed record AdminViewingReport(DateTime FromUtc, DateTime ToUtc, DateTime GeneratedAtUtc, ViewingReportSummary Summary,
    IReadOnlyList<MovieViewingReport> Movies, int TotalMovies, int Page, int PageSize);
