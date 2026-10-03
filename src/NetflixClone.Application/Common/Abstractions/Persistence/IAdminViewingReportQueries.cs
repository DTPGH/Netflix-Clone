using NetflixClone.Application.Admin.Reports;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface IAdminViewingReportQueries
{
    Task<ViewingReportData> ReadAsync(ViewingReportPeriod period, DateTime asOfUtc, int maxMovies, CancellationToken ct);
}
