# Admin dashboard MVP

GET /api/admin/dashboard?from=2026-10-01&through=2026-10-06 uses inclusive Vietnam calendar dates. Both dates omitted defaults to 30 days through today. Maximum 366 days, no future end dates. The client page is /admin, InteractiveWebAssembly without prerendering.

The controller uses Authorize(Admin), validated sub and no-store HTTP headers. Application rechecks the account, email confirmation, lock and actual Admin role on every request before Infrastructure consults its two-minute in-process cache. JWT roles and frontend visibility alone do not grant access.

Metrics:
- New accounts: all UserAccounts created in the selected UTC+7 dates, regardless of roles or email confirmation.
- Effective accounts: distinct account IDs with Active subscriptions whose StartDate <= GeneratedAtUtc < EndDate. This is an as-of count, independent of the chosen historical period and Plan.IsActive.
- Simulated revenue: sum of stored PaymentTransactions.Amount, Succeeded + Simulated, by PaidAt. Currency is VND by current application convention, not a database currency column.
- Qualified views: IsQualifiedView sessions by StartedAt, including soft-deleted profiles/movies and repeat sessions.
- Two daily charts: revenue by PaidAt and views by StartedAt, UTC+7. Missing dates receive zero. Exact values are available in expandable tables.
- Top 10: qualified sessions grouped by movie, ordered by count descending then movie ID. Average time and Finished percentage use only qualified sessions. Finished represents the player's end event, not verified full viewing.

Read-only SQL aggregates/projections run sequentially on the scoped DbContext. No SaveChanges, schema changes, jobs, Redis, packages or EF generated-file changes. Cache keys contain the date range; cached responses retain their original generation timestamp. Each API instance has its own cache. Cold concurrent requests may each calculate a snapshot; cache does not provide single-flight coordination. Queries can see slightly different commits; this is not a transactionally consistent accounting snapshot. Historical sessions can accumulate time/qualify after their start date, so previous dates can change.

Verification: build solution and run tests/NetflixClone.Playback.Specs. Dashboard checks cover current permissions before a cache hit, date validation, cached timestamp, production SQL Server Top 10 translation, and HTTP anonymous/User/Admin access. No real SQL Server query execution or browser visual test is claimed.

Manual tests: open /admin as Admin; deny User/anonymous; remove Admin role while JWT remains valid; select one day/default 30 days/empty range; buy simulated subscription and wait two minutes; expire subscription; play >=120 seconds/replay/reach end; check Top 10 and historical deleted movie; resize mobile/desktop and inspect chart tables. Compare SQL results near UTC+7 midnight. Show dashboard twice within two minutes to verify unchanged GeneratedAtUtc, then reload after expiry.

Files: Application/Admin/Dashboard/AdminDashboard.cs; Infrastructure/Persistence/Queries/AdminDashboardQueries.cs and DependencyInjection.cs; Api/Controllers/AdminDashboardController.cs and Program.cs; Web.Client/Models/AdminDashboardModels.cs, Services/AdminDashboardApiClient.cs, Components/DashboardChart.razor, Pages/AdminDashboard.razor, Program.cs and Components/AuthShell.razor; tests/NetflixClone.Playback.Specs/AdminDashboardChecks.cs and Program.cs; this document.
