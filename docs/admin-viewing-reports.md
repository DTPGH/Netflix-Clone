# Admin viewing reports and Excel export

## Entry points

- WebAssembly page `/admin/reports/views`, linked under the existing Admin menu.
- `GET /api/admin/reports/views?fromUtc=2026-10-01T00:00:00Z&toUtc=2026-10-04T00:00:00Z&page=1&pageSize=20`.
- `GET /api/admin/reports/views/export?fromUtc=2026-10-01T00:00:00Z&toUtc=2026-10-04T00:00:00Z` returns a real `.xlsx` attachment, not CSV with an Excel extension.

Both API endpoints require `[Authorize(Roles = "Admin")]`. Application additionally checks current database account eligibility and assigned Admin role. A locked/unconfirmed account or one whose role was removed cannot export using an older JWT. The controller obtains the actor only from validated `sub`.

## Metric definitions

The selected cohort consists of sessions whose `StartedAt` is in `[fromUtc, toUtc)`. Counts and watched time are their **current recorded totals**, even if their viewing continued beyond the selected dates. This schema cannot reconstruct historical daily watch time or when a session first qualified. There is no `QualifiedAt` or immutable event log. A view is attributed to its session start date.

- Sessions: all sessions in the cohort, including those below 120 seconds.
- Qualified views: sessions with `IsQualifiedView = true`, once per session. Replays/reopened playback are independent sessions.
- Unique profiles: distinct profiles across the entire cohort. Per-movie distinct profiles cannot be summed to get this figure.
- Watched seconds/hours: recorded `WatchedSeconds`, not movie duration or WatchHistories position.
- Finished: `EndReason = Finished` (a reported end event; seeking can cause it, so it is not proof of watching every second).
- Active: no EndedAt and a checkpoint within the last two minutes, evaluated using IClock.
- Timed out: explicit Timeout plus unclosed sessions whose last checkpoint is at least two minutes old. Reports only infer status; they never update EndedAt or any database row.
- Deleted movies and profiles remain included. A movie row has its current title/deletion status, not an immutable historical title. Only movies with sessions appear.

Numbers currently describe the project's demo footage. They are client telemetry, not audited billing or fraud-proof evidence.

## Range, paging and live-data limits

The UI defaults to the last 30 calendar days, includes both selected days, and converts Vietnam UTC+7 boundaries to UTC. UI dates start in 2000 or later. API requires UTC timestamps (Z) and a positive range of at most 366 days. Page size is 1–100, default 20. Movies sort by qualified views descending, sessions descending, then ID ascending.

SQL groups/sums using projection and AsNoTracking; the Application loads at most 10,001 aggregate movie rows to enforce a 10,000-movie limit, derives totals from that bounded set, and pages those rows. No individual sessions/entities are sent to the browser. Above the limit, both endpoints reject the request with `AdminReports.TooLarge`; export never silently truncates. Query dates and row limits also bound accidental huge exports.

The grouped movie query and global distinct-profile query run separately with normal read-committed isolation. Results are approximate live totals, not a transactionally consistent historical snapshot; concurrent playback can change counts between reads. Refreshing/paging reruns the report. Export also reruns it, so it can differ from the previously displayed totals. GeneratedAtUtc indicates query preparation time, not a frozen database version. No heavy reporting locks/snapshot-isolation prerequisite are introduced.

## Excel

Infrastructure implements `IViewingReportExporter` with the small SpreadsheetML subset needed here, using .NET XmlWriter and ZipArchive. Application knows only report models and bytes, not Excel/EF/HTTP types. No package is added. The two worksheets are Summary (UTC range, metadata and totals) and Movies (all aggregate rows, including other pages). Headers are styled, top row frozen, and Movies has filters. Numbers are numeric cells; dates are explicit UTC ISO strings.

All titles are written as XML-escaped `inlineStr` cells. Leading `=`, `+`, `-`, `@` cannot become formulas; invalid XML control characters are removed while Unicode is retained. No profile IDs/names, emails, IPs, device identifiers, tokens or secrets are exported. GET report/export performs no SaveChanges and no database writes. These read-only actions do not create AdminActionLogs entries.

The client uses the existing coordinated AuthSession for authenticated HTTP, including retry after unauthorized responses. Tokens are kept in Authorization headers, not download URLs. The returned bytes travel via DotNetStreamReference to a browser Blob download; object URLs are released. Export uses the loaded report's date range, even if the user has edited form dates without pressing Show report. Component generation guards discard responses after logout/access changes/disposal.

## Files

New:
- Application/Admin/Reports/AdminViewingReportModels.cs, AdminViewingReportUseCase.cs.
- Application/Common/Abstractions/Persistence/IAdminViewingReportQueries.cs.
- Application/Common/Abstractions/Reporting/IViewingReportExporter.cs.
- Infrastructure/Persistence/Queries/AdminViewingReportQueries.cs.
- Infrastructure/Reporting/ViewingReportExcelExporter.cs.
- Api/Controllers/AdminViewingReportsController.cs.
- Web.Client/Models/AdminViewingReportModels.cs.
- Web.Client/Services/AdminViewingReportsApiClient.cs.
- Web.Client/Pages/AdminViewingReports.razor.
- Web.Client/wwwroot/js/report-download.js.
- tests/NetflixClone.Playback.Specs/AdminViewingReportChecks.cs.
- docs/admin-viewing-reports.md.

Modified: Api/Program.cs, Infrastructure/DependencyInjection.cs, Web.Client/Program.cs, Web.Client/Components/AuthShell.razor, tests/NetflixClone.Playback.Specs/Program.cs. Existing viewing-session changes in the working tree are preserved. No SQL/ERD/generated EF/IUnitOfWork changes are needed for reports. Database script 08 from the previous feature must already have been applied.

## Verification / manual tests

Build the solution; run the existing Playback.Specs harness. It checks Application permission/range/paging/export limits, current DB roles, SQL Server LINQ translation (without connecting), XLSX XML/style/string safety and all-row export. HTTP checks exercise anonymous/User/Admin access, UTC binding, validation, JSON and Excel attachment headers using fake persistence. This does not execute production aggregates against SQL Server or open Excel in a real browser.

Manually: sign in as Admin, choose a range around your test sessions, compare qualified/unqualified counts, pause/timeout/replay sessions, include a deleted movie, change page, export and open in Excel/LibreOffice, verify both sheets/all rows and Unicode titles, test no-data and invalid ranges, deny User access, remove current Admin role/lock account and confirm export is denied, and ensure no late report is displayed after logout. Profile filters for children do not apply to administrative historical aggregates.

Out of scope: daily trends without an event history, per-user tracking UI, report scheduling, automatic email delivery, billing, chart libraries and background timeout cleanup.
