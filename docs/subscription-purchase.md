# Subscription purchase and simulated payments

## User flow

Open `/subscription` from authenticated navigation. When no effective subscription exists, choose an offered plan and explicitly confirm a simulated payment. No money is charged, no bank/card information is collected and no external provider is involved. Admin prices now display VND per 30 days.

StartDate=IClock.UtcNow, EndDate=StartDate+30 days, Status=Active, AutoRenew=false. Effective means `Status == Active && StartDate <= now && now < EndDate`. Another purchase is blocked until expiry. Reads infer Expired without writing; a later successful purchase marks old expired Active rows Expired in its same save. Active rows without EndDate, future Active rows or multiple effective rows cause an internal data-integrity failure instead of allowing a new purchase. Plan quality/stream settings remain configuration; stream/quality enforcement, upgrades, automatic renewal, cancellation, refunds and real payments are out of scope.

## API

All routes require `[Authorize]`. Account ID comes only from validated JWT sub. Application verifies the account exists, is email-confirmed and unlocked. Subscriptions belong to accounts, not profiles.

- `GET /api/subscriptions/plans`: offered plans, backend price in VND, duration 30 days, quality/streams and exact UpdatedAtUtc version.
- `GET /api/subscriptions/current`: `{ current, latest }`; current effective subscription or null and latest historical record when none is effective.
- `GET /api/subscriptions/payments?page=1`: own account's transactions, fixed page size 20, CreatedAt/Id descending. Returns historical Amount, status, method, code and timestamps, never persistence entities.
- `POST /api/subscriptions/purchase`: `{ planId, idempotencyKey, expectedPlanUpdatedAtUtc }`; key is a nonempty GUID, version is the exact UTC version displayed. Client price/account ID are not used. Returns `{ subscription, payment, replayed }` with 200 for both new success and replay.

409: SubscriptionAlreadyActive, PlanChanged, PlanUnavailable, IdempotencyConflict, ConcurrentChange. Invalid request=400; ineligible account=403; malformed subscription data=500. Database failures are not disguised as AlreadyActive or successful payment.

## Atomicity and concurrency

An explicit ReadCommitted transaction takes the account row with parameterized `UPDLOCK,HOLDLOCK`. All purchase requests use the same scoped NetflixCloneDbContext through scope/repository/IUnitOfWork. The SQL lock serializes this account across API instances.

After validating eligibility, look up `SIM-{accountId}-{guid:N}` in the existing unique TransactionCode column. A Succeeded Simulated payment for the same account/PlanId returns the original result without writes, before checking subscription/plan availability/version. PlanId defines operation identity; plan version is only a precondition for a new purchase. Reuse with another PlanId conflicts. A key still means its original purchase after expiry; new purchases need new keys. The backend generates TransactionCode, not the client.

For a new key: inspect Active subscriptions, block effective subscriptions, acquire a shared `HOLDLOCK` on the chosen Plan, reload tracked values under that lock, check IsActive/version, then use Plan.Price. Reload prevents an expired subscription's previously tracked Plan from supplying stale values. The Plan lock blocks Admin edits/deactivation until commit and interoperates with the existing Admin plan mutation table lock.

Create Subscription and Succeeded PaymentTransaction with Amount=backend Plan.Price, Method=Simulated, PaidAt/CreatedAt=now. Connect via PaymentTransaction.Subscription; EF temporary keys and insertion ordering fill SubscriptionId in **one SaveChangesAsync**, together with any expired-state updates. Then commit. Dispose rolls back an uncommitted scope. PersistenceConcurrencyException maps locally to ConcurrentChange without retry. IUnitOfWork/generated EF remain unchanged.

No SQL/ERD structural change is needed. DBML notes document the reuse of TransactionCode and VND. Direct SQL or future code ignoring the account lock can still create overlaps. Before real provider integration, separate request idempotency from provider references. Timestamp values are explicitly UTC rather than relying on sysdatetime defaults.

## Browser recovery

InteractiveWebAssembly without prerendering calls API through the coordinated AuthSession. SubscriptionPurchaseDraftStore uses per-account sessionStorage for non-secret PlanId/name/display price, GUID, version and Submitted flag. No password/token/card information is stored there. DisplayPrice is never sent as the charge amount.

Draft is saved before a request and marked Submitted before sending. Unknown/network/500 outcomes retain the key and offer Retry/check same purchase, preventing another selection. Reload/navigation in that tab recovers the draft; page load does not automatically purchase. Success or definitive business rejection clears it. Closing the tab may discard sessionStorage; backend still prevents duplicate effective subscriptions from another key/tab. Generation guards discard late UI results after logout/account changes. Unauthorized HTTP can be retried after coordinated refresh using unchanged key/payload.

## Files

New:
- Application/Subscriptions/SubscriptionModels.cs, SubscriptionUseCase.cs.
- Application/Common/Abstractions/Persistence/ISubscriptionRepository.cs (repository and purchase scope interfaces).
- Infrastructure/Persistence/SubscriptionPurchaseScopeFactory.cs, Repositories/SubscriptionRepository.cs.
- Api/Contracts/Subscriptions/PurchaseSubscriptionRequest.cs, Controllers/SubscriptionsController.cs.
- Web.Client/Models/SubscriptionModels.cs, Services/SubscriptionsApiClient.cs, SubscriptionPurchaseDraftStore.cs, Pages/Subscription.razor, wwwroot/js/subscription-purchase.js.
- tests/NetflixClone.Playback.Specs/SubscriptionChecks.cs, tests/subscription-purchase-store.mjs.
- docs/subscription-purchase.md.

Modified: Api/Program.cs, Infrastructure/DependencyInjection.cs, Web.Client/Program.cs, Web.Client/Components/AuthShell.razor, Web.Client/Pages/AdminPlans.razor, tests/NetflixClone.Playback.Specs/Program.cs, docs/netflix-clone-erd.dbml (notes only), docs/admin-plans.md. Prior uncommitted features are preserved.

## Verification

Build solution, run existing Playback.Specs harness and `node tests/subscription-purchase-store.mjs`.

Checks cover backend amount, 30-day/end boundary, one save, replay/old key/other plan, expiry, changed quote, eligibility, duplicate calls through **fake** serialized scopes, rollback and actual EF temporary-FK tracking. HTTP checks exercise controller/JWT ownership, ignored client account/amount, User access, retry/current/history with fake persistence. Storage checks cover unchanged keys, account isolation/corruption. These do not prove real SQL Server locking and do not replace real browser tests.

Manual: Admin activates a plan; User chooses/confirms/cancels; purchase/current/history; repeated POST; simultaneous same/different-key SQL-backed requests; lose first response then retry/reload; deactivate/change quote; locked account; another account's history; expire test subscription and purchase again; logout during requests. No migration or scaffold step is required.
