# Viewing session recording

## Database setup

Apply `database/08-viewing-session-checkpoints.sql` to the existing database before running this feature. For a new database, `01-schema.sql` includes the same columns/index/check. The application does not run SQL migrations at startup. No generated EF files or `IUnitOfWork` have been edited.

Four metadata columns are required to persist checkpoint/idempotency state across requests and API instances: `ClientSessionId`, `CheckpointSequence`, `WatchedMilliseconds`, `LastCheckpointAtUtc`. Infrastructure maps them with EF shadow properties in the custom partial mapping. They are accessed through repository metadata, without EF types in Application. When intentionally scaffolding later, these columns can become normal generated properties; string-based EF entry access still works. Keep the custom concurrency/index configuration.

## Lifecycle

Opening details, obtaining a playback ticket, or loading video metadata creates no viewing session. The first actual `playing` event starts a session. The browser generates a GUID used only for start idempotency, not authentication. The device identifier comes from the existing credential-store abstraction; API hashes it and validates ownership/non-revocation. Raw identifiers are not persisted in ViewingSessions or logged by this implementation.

Every 15 seconds the player sends cumulative **wall-clock playback milliseconds**, a sequence, and optionally `Finished` or `Closed`. Pause continues heartbeats but grants no watched time. Waiting/buffering and seeking do not grant time; playback rate is accounted for. Samples cap long timer gaps to two seconds conservatively, so sleeping/throttled browsers can undercount instead of granting offline time. The first start-request latency is not credited.

Requests are coordinated in the browser. Backend ignores already-accepted/older sequences. New counters must not decrease or exceed server elapsed time (with a two-second clock/network sampling tolerance); inconsistent additions are rejected. End state is immutable. At 120 seconds `IsQualifiedView` becomes true and remains true; a qualified session counts once, regardless of its final EndReason. Replay or reopening the player creates a new independent session; pause/resume and playback/JWT renewal retain the same session. WatchHistories remains separate resume-position state.

After two minutes without an accepted checkpoint, subsequent checkpoint closes that session with `Timeout`, at its last accepted checkpoint, without granting disconnected time. Starting playback also closes stale sessions for that device. There is no background cleanup job: an untouched abandoned database row may remain open until the next device activity. Future reports must derive effective timeout from LastCheckpointAtUtc rather than treating every null EndedAt as live. Backgrounded playback that resumes after timeout starts a fresh session on subsequent player activity.

Navigation closes the session best-effort. Tab/browser close, profile changes, logout, process crashes or loss of access can prevent a final authenticated checkpoint; timeout covers these cases. A missed final checkpoint may undercount up to the unsaved interval. Recording failure warns without preventing playback. Browser telemetry remains manipulable and is not billing/fraud-proof evidence.

## Persistence and concurrency

An explicit short transaction obtains the owned active Device row with parameterized `UPDLOCK,HOLDLOCK`. All start/checkpoint operations use this scope and the same scoped DbContext as repository and IUnitOfWork. This serializes same-device operations across API instances and blocks device revocation during commit. The unique filtered index `(DeviceId, ClientSessionId)` reinforces start idempotency. CheckpointSequence and EndedAt also have custom optimistic concurrency mapping; a persistence conflict returns a local 409 without retry. Real database errors are not disguised as successful recording. One SaveChanges commits session plus stale-session changes, followed by transaction commit; disposal rolls back an uncommitted scope.

## API

- `POST /api/profiles/{profileId}/movies/{movieId}/viewing-sessions`: `{ deviceIdentifier, clientSessionId }`.
- `PUT /api/profiles/{profileId}/movies/{movieId}/viewing-sessions/{sessionId}/progress`: `{ deviceIdentifier, sequence, watchedMilliseconds, endReason }`.
- Response: `{ sessionId, watchedSeconds, isQualifiedView, isEnded, sequence }`.

Both require JWT authentication and current account/profile/movie playback eligibility, plus an owned active device. UserAccountId is taken only from JWT `sub`. A final checkpoint closes the session atomically, so there is no separate end endpoint/request to race with progress. No Admin reports, concurrent-stream limits, subscription authorization or cleanup jobs are included.

## Verification

Build: `dotnet build NetflixClone.slnx --no-restore`.

Application/fake-persistence and EF mapping checks: `dotnet run --project tests/NetflixClone.Playback.Specs --no-restore`.

Simulated browser-event checks: `node tests/viewing-session-player.mjs`.

These do not verify real SQL Server locking or real browser behavior. Manually verify: no row before Play; qualification after 120 credited seconds; pause/seek do not increase time; resume/renewal keep Id; replay/reopening generates new Id; navigation closes; disconnected sessions timeout; retry does not double count; revoked devices/other account's profiles are rejected. Use two simultaneous HTTP requests against SQL Server to verify start/checkpoint serialization.
