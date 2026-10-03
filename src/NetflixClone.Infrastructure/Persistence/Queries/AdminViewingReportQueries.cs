using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Admin.Reports;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Viewing;
namespace NetflixClone.Infrastructure.Persistence.Queries;
public sealed class AdminViewingReportQueries(NetflixCloneDbContext db) : IAdminViewingReportQueries
{
    public async Task<ViewingReportData> ReadAsync(ViewingReportPeriod period, DateTime asOfUtc, int maxMovies, CancellationToken ct)
    {
        // Historical data is intentionally retained for soft-deleted movies/profiles.
        var sessions = db.ViewingSessions.AsNoTracking().Where(s => s.StartedAt >= period.FromUtc && s.StartedAt < period.ToUtc);
        var rows = await MovieRows(period, asOfUtc, maxMovies).ToListAsync(ct);
        // Distinct across the entire report; summing per-movie profiles would double count.
        var profiles = await sessions.Select(s => s.ProfileId).Distinct().LongCountAsync(ct);
        return new(rows, profiles);
    }
    private IQueryable<MovieViewingReport> MovieRows(ViewingReportPeriod period, DateTime asOfUtc, int maxMovies)
    {
        var cutoff = asOfUtc - ViewingSessionUseCase.Timeout;
        return db.ViewingSessions.AsNoTracking().Where(s => s.StartedAt >= period.FromUtc && s.StartedAt < period.ToUtc)
            .GroupBy(s => new { s.MovieId, s.Movie.Title, s.Movie.IsDeleted })
            .OrderByDescending(g => g.LongCount(s => s.IsQualifiedView)).ThenByDescending(g => g.LongCount()).ThenBy(g => g.Key.MovieId)
            .Take(maxMovies)
            .Select(g => new MovieViewingReport(g.Key.MovieId, g.Key.Title, g.Key.IsDeleted,
                g.LongCount(), g.LongCount(s => s.IsQualifiedView), g.Select(s => s.ProfileId).Distinct().LongCount(),
                g.Sum(s => (long)s.WatchedSeconds), g.LongCount(s => s.EndReason == "Finished"),
                g.LongCount(s => s.EndedAt == null && EF.Property<DateTime>(s, "LastCheckpointAtUtc") > cutoff),
                g.LongCount(s => s.EndReason == "Timeout" || s.EndedAt == null && EF.Property<DateTime>(s, "LastCheckpointAtUtc") <= cutoff)));
    }
}
