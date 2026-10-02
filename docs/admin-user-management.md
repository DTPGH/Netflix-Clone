# Admin user management MVP

## Database prerequisite

Run database/06-extend-admin-action-log-actions.sql on the existing NetflixCloneDb.
It expands CK_AdminActionLogs_Action to allow RoleAssigned and RoleRemoved,
alongside AccountLocked and AccountUnlocked. It changes no columns or EF entities.
The full creation script and DBML contain the same action values.
Do not rerun the full 01-schema.sql against an existing database.

## API and UI

- GET /api/admin/users: paged email search and optional manual-lock filter.
- GET /api/admin/users/roles: configured User/Admin role IDs, without hard-coded IDs.
- GET /api/admin/users/{userId}: safe account details and exact UpdatedAtUtc.
- PUT /api/admin/users/{userId}/lock: IsLocked, Reason, ExpectedUpdatedAtUtc.
- PUT /api/admin/users/{userId}/roles: complete RoleIds array, Reason, ExpectedUpdatedAtUtc.
- GET /api/admin/action-logs: paging, actorId, targetId and action filters.

All endpoints require Authorize(Admin) plus current database Admin access.
The actor comes only from JWT sub. Request reasons are required, trimmed,
and limited to 400 characters so a role name can fit into the 500-character log Reason.
No password hashes, authentication tokens or entities are returned.
User and Admin are the supported MVP roles; there is no custom-role CRUD.

Open /admin/users and /admin/action-logs. Changes have an explicit confirmation.
Exact UpdatedAtUtc protects an outdated editor. A 409 or uncertain response
requires reloading before another mutation; write requests are not automatically retried.

## Persistence and concurrency

AdminUserMutationScopeFactory uses the same scoped DbContext as the repository
and IUnitOfWork. A ReadCommitted transaction first acquires UPDLOCK/HOLDLOCK on
the existing Admin Roles row, then on the target UserAccounts row.
Every account/role mutation in this feature uses that same gate, serializing
last-eligible-admin checks across API instances. The target lock protects its
read and write against other account writers for the transaction duration.
Authorization and version checks occur after locks are acquired.

Self-lock and self-removal of Admin are rejected. At least one unlocked,
email-confirmed Admin must remain. Every real change advances UpdatedAt.
Role removals are explicit EF deletes; assignment timestamps are UTC.
One SaveChangesAsync persists account/role changes and their audit rows,
then the explicit transaction commits. Failure/disposal rolls back.
No-op mutations produce no logs. Database timeouts/deadlocks are not
translated into a successful result or business conflict.
Locks can serialize unrelated Admin mutations; this is an intentional MVP trade-off.

Logs are append-only through this API and contain successful changes only.
Role changes produce one row per assigned/removed role with its name in Reason.
This feature does not audit movie/collection CRUD, failed requests or login activity.

## Authentication effects

Manual lock stays separate from FailedLoginCount/LockoutEnd and does not reset them.
Login and refresh reject locked accounts. Existing access tokens are not revoked
by this feature; endpoints checking only JWT claims can accept them until expiry.
Admin use cases check current account eligibility and role membership.
Role claims change on the next successful login/refresh, not in an already-issued token.
Unlocking does not revoke old refresh tokens or devices.

## Manual checks

1. Search users and inspect an account without secret fields.
2. Grant/remove Admin on another account and inspect per-role logs.
3. Lock/unlock an account and test Login/Refresh while locked.
4. Repeat unchanged state: no duplicate audit rows.
5. Save from two old editors: second request receives 409.
6. Try self-lock/self-demotion and verify rejection.
7. Use an existing JWT after Admin removal: Admin requests fail current-role checks.
8. Filter logs by actor, target and action; follow the target account link.

Automated use-case checks use fake persistence and do not prove SQL Server
lock behavior or transaction rollback under a live multi-instance race.
