# Watch History and Continue Watching MVP

## Behavior

Progress belongs to (ProfileId, MovieId), with one existing unique database row.
Watch reads saved progress before preparing the player. Incomplete movies resume
at the last stored position; completed movies start from zero. A metadata load
alone does not create history.

The player reports about every 15 seconds during playback, on pause/end, when
the tab becomes hidden, and before internal Blazor navigation. Internal navigation
awaits the checkpoint attempt. Abrupt tab closure, browser termination, connection
loss or logout can lose the last unsaved seconds; no unload/beacon guarantee is
claimed. Logout/profile changes stop new reporting, and late responses are ignored.
An already-sent request may still commit to its original profile.

Continue Watching appears on Browse, with up to 12 recent incomplete, nonhidden
entries with positive progress. It filters deleted/unavailable/over-age movies and
deleted profiles before Take. Existing records remain when profile maturity drops;
they become visible again if eligible. Completed rows remain in the database but
leave the rail. Replaying and saving new progress clears completion and returns
the movie to the rail. Backward seeks save the chosen position, not a maximum.

## Demo duration decision

Movies.DurationSeconds describes the real catalog movie; the shared Blender MP4
has a different duration. Checkpoints therefore contain the player's actual media
duration. API accepts integer seconds: duration 1–86400, position 0–duration.
Ended=true is valid only within two seconds of that duration. Completion occurs
on the actual ended event, not at an arbitrary catalog percentage.

The rail displays elapsed position, not a misleading percentage of catalog runtime.
The existing schema does not store media duration/version. A resume position beyond
a changed asset is clamped by the player. Replacing assets does not automatically
reset history in this slice.

Client position/duration/ended are untrusted convenience data, not proof of watching,
qualified views, billing data or entitlement. The API still verifies playback
eligibility on every GET/PUT. No ViewingSession, analytics or recommendation signal
is created.

## API

All routes require Authorize and no-store caching. Account comes exclusively from
JWT sub. Ownership and age use current database profile state.

- GET /api/profiles/{profileId}/watch-history/{movieId}
  -> { movieId, positionSeconds, isCompleted, updatedAtUtc }.
  An eligible unwatched movie returns zero/false/null without writing.
- PUT /api/profiles/{profileId}/watch-history/{movieId}
  body { positionSeconds, durationSeconds, ended, expectedUpdatedAtUtc }.
  -> same saved progress response. Initial expectedUpdatedAtUtc is null; subsequent
  writes send the exact UTC timestamp returned by the preceding GET/PUT.
- GET /api/profiles/{profileId}/continue-watching?limit=12
  -> { items: [{ movie: MovieSummary, positionSeconds, lastWatchedAtUtc }] }.
  Limit 1–24. Order: LastWatchedAt descending, Id descending.

Invalid progress is 400. Unknown/ineligible profile or movie is 404; unavailable
playback can return the existing 409. A stale progress version or persistence
concurrency conflict returns WatchHistory.ConcurrentChange (409).
Account exists, confirmed email, manual lock, ownership, soft-delete, maturity and
playback eligibility are reused from IGetMoviePlaybackUseCase for GET/PUT.
Continue-list uses existing account authorization/profile ownership semantics.

## Persistence and concurrency

IWatchHistoryRepository never saves. Each effective checkpoint uses one
IUnitOfWork.SaveChangesAsync. Server clock supplies CreatedAt/LastWatchedAt/UpdatedAt.
CreatedAt remains unchanged. UpdatedAt always advances even with equal/backward
clock readings. CompletedAt and IsCompleted are changed together to satisfy the
existing SQL check. Watching explicitly sets IsHidden=false, although hide/manage
history actions are not part of this slice.

Two layers protect progress:
1. Compare expectedUpdatedAtUtc with the current row before mutation. A stale tab
   cannot silently replace a newer checkpoint after reading it.
2. Custom partial EF mapping makes WatchHistory.UpdatedAt a concurrency token,
   covering concurrent changes between read and save. The specific duplicate
   profile/movie constraint on a single added history row is also translated into
   PersistenceConcurrencyException in the existing custom SaveChanges override.

Only this use case maps these conflicts to its 409. No retry, no global HTTP
mapping, no IUnitOfWork change. Other database failures propagate. Failed EF saves
retain the standard transaction rollback behavior.

Rows store datetime2 without timezone metadata. Responses explicitly mark those
server-written UTC timestamps as UTC, preserving ticks for optimistic concurrency.
The browser serializes saves and updates its timestamp only after confirmed success.
Uncertain outcomes/conflicts disable further writes until Reload saved progress;
video can continue playing, but the UI states progress is not confirmed.

## Client lifecycle

demo-player.js queues only the latest pending sample while a checkpoint is in flight,
so an ended event is not lost behind a periodic checkpoint. Watch also serializes
writes and checks component/profile generations before and after HTTP calls.
Ticket replacement preserves progress/resume state and does not produce artificial
pause writes. Renewing after completion does not clear completion until playback
actually starts again.

Progress responses are not browser credentials; they remain in component memory.
Existing AuthSession handles JWT refresh, and no access/refresh/playback tokens
are stored or logged by the history feature.

## Verification

    dotnet build
    dotnet build tests/NetflixClone.Playback.Specs --no-restore
    dotnet run --project tests/NetflixClone.Playback.Specs --no-build
    node tests/NetflixClone.Playback.Specs/player-checks.mjs

Build: zero warnings/errors. Existing 28 playback HTTP checks plus 20 history HTTP
checks pass using real controllers/authentication and fake persistence. JS checks
cover resume, no metadata writes, serialized pause/end, renewal after completion
and cleanup. Real SQL concurrency and browser MP4 playback remain manual checks.

Manual checklist:
1. Play for over 15 seconds, pause, return to Browse: Continue Watching appears.
2. Select its card: resume close to the saved position.
3. Switch profile: no sharing or late response from the previous profile.
4. Seek backward and pause: resume from that position on next visit.
5. Reach the end: completion timestamp exists and the item leaves the rail.
6. Replay: new progress clears completion and returns it to the rail.
7. Change to Kids: over-age entries disappear without deleting history.
8. Open two tabs: a stale checkpoint returns 409; reload resolves the conflict.
9. Disconnect network/lose a PUT response: visible warning, no automatic overwrite.
10. Refresh tickets across five minutes: no position reset or synthetic history write.
11. Close the tab abruptly: last confirmed checkpoint survives; the tail may be lost.

No schema/generated EF/SQL changes. No hide/delete-history UI, full history archive
page, ViewingSessions, view counts, recommendations based on history or subscriptions.

## File inventory

Added:
- src/NetflixClone.Application/Viewing/WatchProgressModels.cs
- src/NetflixClone.Application/Viewing/WatchProgressUseCase.cs
- src/NetflixClone.Application/Common/Abstractions/Persistence/IWatchHistoryRepository.cs
- src/NetflixClone.Infrastructure/Persistence/Repositories/WatchHistoryRepository.cs
- src/NetflixClone.Api/Contracts/Viewing/WatchProgressRequest.cs
- src/NetflixClone.Api/Controllers/WatchHistoryController.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Services/WatchHistoryApiClient.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Components/ContinueWatching.razor
- tests/NetflixClone.Playback.Specs/WatchHistoryChecks.cs
- docs/watch-history.md

Modified:
- src/NetflixClone.Api/Program.cs: register use case.
- src/NetflixClone.Infrastructure/DependencyInjection.cs: register repository.
- src/NetflixClone.Infrastructure/Persistence/NetflixCloneDbContext.Concurrency.cs:
  history timestamp mapping.
- src/NetflixClone.Infrastructure/Persistence/NetflixCloneDbContext.SaveChanges.cs:
  translate history insert race.
- src/NetflixClone.Web/NetflixClone.Web.Client/Program.cs: register client.
- src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Browse.razor: rail.
- src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Watch.razor: resume/save lifecycle.
- src/NetflixClone.Web/NetflixClone.Web.Client/wwwroot/js/demo-player.js: checkpoints.
- tests/NetflixClone.Playback.Specs/Program.cs: fake history persistence/HTTP setup.
- tests/NetflixClone.Playback.Specs/player-checks.mjs: progress lifecycle checks.

The test directory was already untracked before this work. No commits or file removals.
