# Admin plans MVP

Open /admin/plans from the Admin navigation. Prices are VND per 30 days, matching the simulated subscription purchase lifetime.
Create a plan, review its configuration, then activate it in a separate confirmed action.
Creation always sets IsActive=false; no delete endpoint is provided.

## Contracts

- GET /api/admin/plans: all plans with subscription counts, ordered by price and ID.
- GET /api/admin/plans/{id}: configuration and exact UpdatedAtUtc.
- POST /api/admin/plans: Name, Price, MaxConcurrentStreams, MaxQuality.
- PUT /api/admin/plans/{id}: those fields plus ExpectedUpdatedAtUtc.
- PUT /api/admin/plans/{id}/status: IsActive and ExpectedUpdatedAtUtc.

All endpoints require JWT Admin authorization and current database Admin eligibility.
Names are trimmed, at most 100 characters, and unique according to SQL Server collation.
Price must fit decimal(18,2), be non-negative and have no fractional precision beyond cents.
Streams range from 1 to 10; qualities are 480p, 720p, 1080p and 4K.
The plan does not change the account's independent five-active-profile limit.

Any subscription reference, regardless of status, makes configuration immutable.
Create a new plan to offer changed terms, and deactivate the previous plan if appropriate.
IsActive only controls whether a plan is offered for future checkout; it does not cancel subscriptions.
This feature does not implement checkout, payment, renewals or playback restrictions.
AdminActionLogs remains an account-management audit log and is not used for plans.

## Persistence

Plan.UpdatedAt is an EF concurrency token in the custom mapping and advances on mutations.
ExpectedUpdatedAtUtc rejects a stale editor with 409.
The custom IsActive sentinel ensures false is saved despite the database's true default.
Repositories do not save; each mutation calls IUnitOfWork once.

AdminPlanMutationScopeFactory uses the same scoped DbContext and a short explicit SQL transaction.
TABLOCKX/HOLDLOCK on the small Plans table serializes name checks and mutations across instances,
including creation when the table is empty. It also blocks new subscription FK references
while configuration usage is checked. Reads can wait during this transaction.
Uncommitted transaction disposal rolls back. Database failures are not swallowed.
A future checkout must read current plan state and price in its own transaction.

## Verification

Try creating inactive, activating/deactivating, duplicate names, invalid prices,
two stale editors, and editing a plan referenced by any subscription.
Automated checks cover use-case rules and EF mapping with fake persistence;
they do not exercise live SQL Server locking or browser UI.
