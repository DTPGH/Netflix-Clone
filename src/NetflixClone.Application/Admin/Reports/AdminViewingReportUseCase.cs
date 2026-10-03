using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Reporting;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
namespace NetflixClone.Application.Admin.Reports;
public interface IAdminViewingReportUseCase
{
    Task<Result<AdminViewingReport>> GetAsync(int actorId, ViewingReportPeriod period, int page, int pageSize, CancellationToken ct);
    Task<Result<byte[]>> ExportAsync(int actorId, ViewingReportPeriod period, CancellationToken ct);
}
public sealed class AdminViewingReportUseCase(IUserAccountRepository accounts, IAdminViewingReportQueries queries,
    IViewingReportExporter exporter, IClock clock) : IAdminViewingReportUseCase
{
    public const int MaxMovies = 10000;
    private async Task<Result<AdminViewingReport>> Load(int actorId, ViewingReportPeriod period, int page, int pageSize, CancellationToken ct)
    {
        var actor = await accounts.GetByIdAsync(actorId, ct);
        if (actor is null || actor.IsLocked || !actor.EmailConfirmed ||
            !(await accounts.GetRoleNamesAsync(actorId, ct)).Contains(RoleNames.Admin, StringComparer.Ordinal))
            return Result<AdminViewingReport>.Failure(new("AdminReports.Forbidden", "Current administrator access is required.", ErrorType.Forbidden));
        if (period.FromUtc.Kind != DateTimeKind.Utc || period.ToUtc.Kind != DateTimeKind.Utc ||
            period.ToUtc <= period.FromUtc ||
            period.ToUtc - period.FromUtc > TimeSpan.FromDays(366) || page is < 1 or > 1000000 || pageSize is < 1 or > MaxMovies)
            return Result<AdminViewingReport>.Failure(new("AdminReports.InvalidRange", "Provide a UTC range of at most 366 days and valid paging.", ErrorType.Validation));
        var now = clock.UtcNow;
        var data = await queries.ReadAsync(period, now, MaxMovies + 1, ct);
        if (data.Movies.Count > MaxMovies)
            return Result<AdminViewingReport>.Failure(new("AdminReports.TooLarge", "This report exceeds 10,000 movies. Choose a shorter date range.", ErrorType.Validation));
        var all = data.Movies.OrderByDescending(m => m.QualifiedViews).ThenByDescending(m => m.Sessions).ThenBy(m => m.MovieId).ToArray();
        var summary = new ViewingReportSummary(all.Sum(m => m.Sessions), all.Sum(m => m.QualifiedViews), data.UniqueProfiles,
            all.Sum(m => m.WatchedSeconds), all.Sum(m => m.FinishedSessions), all.Sum(m => m.ActiveSessions), all.Sum(m => m.TimedOutSessions));
        return Result<AdminViewingReport>.Success(new(period.FromUtc, period.ToUtc, now, summary,
            all.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), all.Length, page, pageSize));
    }
    public Task<Result<AdminViewingReport>> GetAsync(int actorId, ViewingReportPeriod period, int page, int pageSize, CancellationToken ct)
        => pageSize is < 1 or > 100
            ? Task.FromResult(Result<AdminViewingReport>.Failure(new Error("AdminReports.InvalidPage", "Page size must be between 1 and 100.", ErrorType.Validation)))
            : Load(actorId, period, page, pageSize, ct);
    public async Task<Result<byte[]>> ExportAsync(int actorId, ViewingReportPeriod period, CancellationToken ct)
    {
        var result = await Load(actorId, period, 1, MaxMovies, ct);
        return result.IsFailure ? Result<byte[]>.Failure(result.Error!) : Result<byte[]>.Success(exporter.Generate(result.Value!));
    }
}
