# Concurrent playback MVP

## Rules and entitlement

The effective Subscription's current Plan.MaxConcurrentStreams limits logical
playback sessions across ALL profiles of the account. Logged-in devices do not
occupy slots by themselves. Two player tabs on one browser count as two streams.

An effective subscription has Status = Active and StartDate <= UtcNow < EndDate.
Plan.IsActive controls sales, not previously purchased access. Entitlements are
not snapshotted: increasing the current Plan limit permits more starts; reducing
it blocks new starts while existing live streams finish/expire. There is no forced
eviction. Multiple effective subscriptions or an invalid configured limit fail
closed as a configuration/data-integrity error.

## Reservation and transaction

POST `/api/profiles/{profileId}/movies/{movieId}/playback` now requires a JSON body
with `deviceIdentifier` and `clientSessionId` (a UUID). The browser obtains the
existing server-issued device identifier through ICredentialStore. It is hashed
before lookup, never used as a fingerprint/IP identity or logged raw.

JWT, profile PIN, account, profile ownership/deletion, movie age/availability and
subscription checks remain. API also checks the private file exists before
reserving a slot. ViewingSessionUseCase.StartAsync then:

1. Begins the existing SQL transaction; locks UserAccounts with parameterized
   UPDLOCK,HOLDLOCK, then the owned non-revoked Device in the existing lock order.
2. Rechecks playback eligibility and expires stale rows for that device.
3. Reads (DeviceId, ClientSessionId). A live retry reuses that session without
   advancing its deadline. A key cannot change profile/movie or reactivate an end.
4. For a new key, loads the effective subscription/current Plan and counts live
   account-wide sessions. At the limit, rejects without insertion or ticket.
5. Adds the session, calls the unchanged IUnitOfWork.SaveChangesAsync once and
   commits. Only then does API issue its protected playback ticket.

Live-count queries require EndedAt IS NULL, LastCheckpointAtUtc > UtcNow - 2 minutes,
an owned non-revoked device and a non-deleted owned profile. They do NOT depend on
IsQualifiedView: the separate 120 credited seconds rule still determines a
reportable view. No cleanup job is required to exclude abandoned slots.

The account lock spans count, insertion, save and commit, serializing admission
across different devices/tabs and multiple API instances sharing SQL Server. Scope,
repositories and IUnitOfWork share one scoped DbContext. There are no production
in-memory locks, Redis or new generic repositories. The existing unique filtered
(DeviceId, ClientSessionId) index additionally protects retry idempotency.

The standalone POST viewing-sessions endpoint uses the same admission path, so
it cannot bypass the limit. Persistence concurrency conflicts preserve rollback
and return ViewingSessions.ConcurrentChange (409). Other database errors are not
misreported as a stream-limit failure. A committed request with a lost HTTP
response is recovered by retrying the same UUID; otherwise its slot expires.

## Contracts and media boundary

Playback response retains movieId, title, videoUrl, contentType, isDemo and
expiresAtUtc. It adds viewingSession containing sessionId, sequence,
watchedMilliseconds, watchedSeconds, isQualifiedView, isEnded and leaseExpiresAtUtc.
The player is seeded with this committed reservation BEFORE assigning its source.
Opening Watch reserves a slot for up to two minutes even before pressing Play.

PlaybackTicketService now uses purpose NetflixClone.PlaybackTicket.v2, requires
a positive SessionId, and retains the existing profile unlock proof. Old tickets
without a session no longer authenticate. The five-minute ticket lifetime is
separate from the two-minute sliding stream lease.

Every media GET/HEAD verifies the ticket's account/profile/movie/session binding,
open state, deadline, profile deletion and device non-revocation. Existing PIN,
account, subscription, age, media-key, filesystem and Range checks remain.

| Error | HTTP | Behavior |
|---|---|---|
| Playback.ConcurrentStreamLimitReached | 409 | No slot; stop another stream or retry later; no ticket |
| Playback.SessionExpired | 409 | Ended reservation key or invalid media lease; no MP4 bytes |
| Movies.SubscriptionRequired | 403 | No effective subscription |
| ViewingSessions.ConcurrentChange | 409 | Conflicting reservation/checkpoint commit |

The session ID is a telemetry/playback identifier, not an authentication secret.
Knowing it alone does not grant ownership or media access.

## Player lifecycle

Each logical player uses a UUID retained for ticket/JWT retries and uncertain
initial outcomes. An explicit page reload/retry can replace a key confirmed ended
by the backend once; background ticket renewal never silently acquires a new slot.
New movies/profiles and replay obtain a new logical session.

While actually playing, the player sends cumulative progress about every 15
seconds. Only POSITIVE watched-millisecond increases advance LastCheckpointAtUtc.
Pause sends a final checkpoint then stops heartbeats; repeated zero-progress
checkpoints and ticket renewals cannot extend its server lease. Pause therefore
holds a slot at most two minutes from the last advancing checkpoint, potentially
a little less than two minutes from the UI pause. Buffering/sleep without
confirmed progress follows the same timeout. Seeking grants no watched time.

The client stops at its last confirmed deadline if checkpoint requests fail. It
checks the deadline on play/visibility return as well, handling throttled timers.
UI offers Try again to reacquire and restore saved resume position; another stream
may have taken the slot. Account access-token refresh remains separate.

Finished/Closed checkpoints release the slot when committed. Immediate replay
waits for its in-flight Finished checkpoint before acquiring a new slot/ticket.
Ticket renewal preserves counters, session and position; stale renewal responses
cannot shorten a deadline updated by a newer checkpoint. Cleanup pauses/removes
the source before awaiting final close, and removes timers/listeners.

Navigation/tab-close release is best-effort. Logout, changed PIN/profile, device
revoke, network loss or process termination can prevent a final authenticated
checkpoint; expiry bounds abandoned slot retention. Revoked devices/deleted
profiles stop counting immediately after their database change. Stale rows may
keep EndedAt NULL until later device activity; live queries exclude them anyway.

## Limits

These are logical leases for the whole-file MP4 demo. An already-open response
or buffered/downloaded bytes cannot be recalled. Sharing a bearer playback URL
can reuse its same logical session until validation fails; this is not physical
screen identification or DRM-grade enforcement. Segmented delivery/stronger
session binding and anti-sharing remain future work. Watched-time telemetry is
conservatively validated client input, not proof a human watched the video.
Backend UTC remains authoritative; client clock/timer differences may interrupt
early. No quality enforcement, stream-management UI, automatic eviction, schema
change, entitlement snapshot, job or Redis is introduced.

## Verification

```powershell
dotnet build
dotnet run --project tests/NetflixClone.Playback.Specs/NetflixClone.Playback.Specs.csproj
dotnet run --project tests/NetflixClone.ProfilePin.Specs/NetflixClone.ProfilePin.Specs.csproj
node tests/viewing-session-player.mjs
node tests/NetflixClone.Playback.Specs/player-checks.mjs
```

Tests cover serialized parallel admission using fake transactional scopes,
retry, tabs/devices/profiles, pause/zero-progress heartbeat expiry, replay,
current Plan changes, revocation and rollback. Real HTTP checks cover request
validation, 409 without a ticket and the media lease gate, alongside existing
playback/history/subscription/PIN regressions. Deterministic JavaScript checks
cover pause, retry, renewal, seek, immediate replay, offline expiry, throttled-tab
resume and cleanup. They do not prove live SQL blocking or actual browser playback.

Manual browser/SQL tests:

1. Plan limit 1: play in one tab/device; another must receive the limit message.
2. Plan limit 2: two tabs of the SAME browser may play; a third is refused across
   every profile on the account.
3. Pause one player: retain its slot before deadline; release after two minutes.
   Resume must reacquire and may fail if someone else took the slot.
4. Watch for several minutes to verify heartbeats. Only 120 credited seconds
   qualify the view; reserving/pausing/seeking alone must not qualify it.
5. Finish/close normally and start elsewhere immediately. Simulate offline or
   abrupt tab close and verify eventual release without a cleanup job.
6. Retry one UUID concurrently (one row); send distinct UUIDs through two API
   instances to confirm SQL-backed account locking never admits beyond the limit.
7. Revoke the playing device and retry media; its slot no longer counts. Recheck
   PIN, manual account lock, kids restrictions and subscription expiry.
8. Increase/decrease current Plan limit and discontinue sales; admission follows
   its current limit, live streams are not evicted and paid access remains valid.
9. Lose the initial response then retry; replay immediately on a one-stream plan;
   verify new replay row/counter without duplicate reservations or inherited time.

## Files changed

Generated EF, SQL/ERD, IUnitOfWork and package references are unchanged.

- [docs/concurrent-streams.md](D:/Cybersoft/NetflixClone/docs/concurrent-streams.md)
- [docs/protected-demo-playback.md](D:/Cybersoft/NetflixClone/docs/protected-demo-playback.md)
- [docs/viewing-sessions.md](D:/Cybersoft/NetflixClone/docs/viewing-sessions.md)
- [src/NetflixClone.Api/Contracts/Catalog/PlaybackResponse.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Contracts/Catalog/PlaybackResponse.cs)
- [src/NetflixClone.Api/Contracts/Catalog/StartPlaybackRequest.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Contracts/Catalog/StartPlaybackRequest.cs)
- [src/NetflixClone.Api/Controllers/MediaController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/MediaController.cs)
- [src/NetflixClone.Api/Controllers/PlaybackController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/PlaybackController.cs)
- [src/NetflixClone.Api/Security/PlaybackTicketHandler.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Security/PlaybackTicketHandler.cs)
- [src/NetflixClone.Application/Common/Abstractions/Persistence/IViewingSessionRepository.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Common/Abstractions/Persistence/IViewingSessionRepository.cs)
- [src/NetflixClone.Application/Common/Abstractions/Security/IPlaybackTicketService.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Common/Abstractions/Security/IPlaybackTicketService.cs)
- [src/NetflixClone.Application/Viewing/ViewingSessionModels.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Viewing/ViewingSessionModels.cs)
- [src/NetflixClone.Application/Viewing/ViewingSessionUseCase.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Viewing/ViewingSessionUseCase.cs)
- [src/NetflixClone.Infrastructure/Persistence/Repositories/ViewingSessionRepository.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Infrastructure/Persistence/Repositories/ViewingSessionRepository.cs)
- [src/NetflixClone.Infrastructure/Security/PlaybackTicketService.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Infrastructure/Security/PlaybackTicketService.cs)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Models/CatalogModels.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Models/CatalogModels.cs)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Watch.razor](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Watch.razor)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Services/CatalogApiClient.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Services/CatalogApiClient.cs)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Services/ViewingSessionsApiClient.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Services/ViewingSessionsApiClient.cs)
- [src/NetflixClone.Web/NetflixClone.Web.Client/wwwroot/js/demo-player.js](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/wwwroot/js/demo-player.js)
- [src/NetflixClone.Web/NetflixClone.Web.Client/wwwroot/js/viewing-session.js](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/wwwroot/js/viewing-session.js)
- [tests/NetflixClone.Playback.Specs/ConcurrentStreamChecks.cs](D:/Cybersoft/NetflixClone/tests/NetflixClone.Playback.Specs/ConcurrentStreamChecks.cs)
- [tests/NetflixClone.Playback.Specs/ConcurrentStreamHttpChecks.cs](D:/Cybersoft/NetflixClone/tests/NetflixClone.Playback.Specs/ConcurrentStreamHttpChecks.cs)
- [tests/NetflixClone.Playback.Specs/player-checks.mjs](D:/Cybersoft/NetflixClone/tests/NetflixClone.Playback.Specs/player-checks.mjs)
- [tests/NetflixClone.Playback.Specs/Program.cs](D:/Cybersoft/NetflixClone/tests/NetflixClone.Playback.Specs/Program.cs)
- [tests/NetflixClone.Playback.Specs/TestStreamSubscriptions.cs](D:/Cybersoft/NetflixClone/tests/NetflixClone.Playback.Specs/TestStreamSubscriptions.cs)
- [tests/NetflixClone.Playback.Specs/ViewingSessionChecks.cs](D:/Cybersoft/NetflixClone/tests/NetflixClone.Playback.Specs/ViewingSessionChecks.cs)
- [tests/NetflixClone.Playback.Specs/ViewingSessionHttpChecks.cs](D:/Cybersoft/NetflixClone/tests/NetflixClone.Playback.Specs/ViewingSessionHttpChecks.cs)
- [tests/NetflixClone.ProfilePin.Specs/Program.cs](D:/Cybersoft/NetflixClone/tests/NetflixClone.ProfilePin.Specs/Program.cs)
- [tests/viewing-session-player.mjs](D:/Cybersoft/NetflixClone/tests/viewing-session-player.mjs)
