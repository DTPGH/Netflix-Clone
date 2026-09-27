# Profile movie ratings

## API and rules

All three operations use /api/profiles/{profileId}/ratings/{movieId}, require
Authorize, and disable response caching. Account identity comes only from JWT sub.
Unknown, foreign and soft-deleted profiles return the same Ratings.ProfileNotFound.

- GET: 200 { movieId, value }; value is null when there is no rating.
- PUT: { "value": "NotForMe" | "Like" | "Love" }; returns 200 { movieId, value }.
- DELETE: 204, including when no rating exists.

GET/PUT require a nondeleted movie. Unavailable movies can still be rated.
DELETE can remove a rating whose movie was soft-deleted. No viewing-history
requirement, aggregate score, My List side effect or preference side effect.
Values are exact strings, not star scores. Missing/invalid values return 400.

Each mutation saves once when needed. An unchanged PUT and an absent DELETE
do not write. CreatedAt is preserved on update; UpdatedAt advances even if the
clock has not advanced. Repositories never commit.

## Concurrency and limitations

Rating.UpdatedAt is a concurrency token in the custom partial EF mapping.
An update/delete includes the original timestamp in its database condition.
A competing change after the read causes PersistenceConcurrencyException,
which only the rating mutation use cases translate into Ratings.ConcurrentChange
(HTTP 409). A concurrent DELETE may also return 409; repeating after reloading
returns 204 if absent.

The existing unique constraint prevents duplicate profile/movie pairs. The custom
SaveChanges override translates only SQL Server 2601/2627 referring to
UQ_Ratings_ProfileId_MovieId with a single added Rating and no other pending
writes. This becomes the same framework-independent concurrency exception.
Two insert requests may express different choices, so the losing insert is never
silently reported as successful. No retries or global HTTP exception mapping.
Other database failures propagate; EF retains its normal failed-save rollback.

This is concurrency protection between database read and commit. No ETag/client
version is included: a later, sequential request can replace an earlier rating
even if its tab displayed stale data. Hard-delete/recreate is a new rating row.
Profile/movie soft deletion can race after validation; this MVP introduces no
cross-entity transaction locks. Subsequent requests recheck visibility/ownership.
No profile-age filtering or playback authorization is introduced by ratings.

## Browser flow

Movie Detail uses RatingControl inside its existing non-prerendered WebAssembly
page. Scoped RatingsApiClient uses AuthSession for token coordination. GET can
retry after a 401 through the existing session mechanism; writes are not
automatically retried.

Sign in and explicit profile selection are required before rating. Controls
display the selected profile, current choice, loading/saving/error states and
Remove rating. They use Bootstrap and aria-pressed, without a new CSS framework.
UI labels stay English to match the existing application.

Every request captures the selected profile generation. Switching profiles,
logging out, changing movies or disposing the component prevents an old response
from updating the new UI. A failed/uncertain write disables further changes until
Reload rating succeeds. A request already sent may still commit to its original
profile; it is never retargeted to a newly selected profile.

## Verification

Run dotnet build, then:

    dotnet run --project tests/NetflixClone.Ratings.Specs --no-build

The package-free executable verifies use-case ownership, validation, timestamps,
idempotency and exception handling using fake persistence. It does not verify
SQL Server error translation, database races or browser rendering.

Manual checks:
1. Anonymous API access returns 401. Foreign/unknown/deleted profile returns 404.
2. Choose profile A; Movie Detail initially shows Not rated yet.
3. Set Like, change to Love, reload and verify persistence; remove and reload.
4. Switch to profile B; ratings remain independent.
5. Invalid/missing rating values return 400, with no write.
6. Unavailable movies can be rated. Deleted movies reject GET/PUT; DELETE works.
7. Run simultaneous PUTs for an unrated pair: one row remains; a losing insert
   returns 409. Exercise overlapping update/update and update/delete.
8. Throttle the network, switch profile/logout while loading/saving: stale
   responses must not repopulate the UI.
9. Lose a save response or provoke 409: Reload rating is required before editing.
10. Expire the access token and verify existing coordinated refresh still works.

## File inventory

Added:
- src/NetflixClone.Application/Common/Abstractions/Persistence/IRatingRepository.cs
- src/NetflixClone.Application/Ratings/RatingModels.cs
- src/NetflixClone.Application/Ratings/RatingErrors.cs
- src/NetflixClone.Application/Ratings/GetRatingUseCase.cs
- src/NetflixClone.Application/Ratings/SetRatingUseCase.cs
- src/NetflixClone.Application/Ratings/RemoveRatingUseCase.cs
- src/NetflixClone.Infrastructure/Persistence/Repositories/RatingRepository.cs
- src/NetflixClone.Api/Contracts/Ratings/RatingContracts.cs
- src/NetflixClone.Api/Controllers/RatingsController.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Services/RatingsApiClient.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Components/RatingControl.razor
- tests/NetflixClone.Ratings.Specs/NetflixClone.Ratings.Specs.csproj
- tests/NetflixClone.Ratings.Specs/Program.cs
- docs/rating-movie.md

Modified:
- NetflixClone.slnx: include rating checks in solution builds.
- src/NetflixClone.Api/Program.cs: register use cases.
- src/NetflixClone.Infrastructure/DependencyInjection.cs: register repository.
- src/NetflixClone.Infrastructure/Persistence/NetflixCloneDbContext.Concurrency.cs:
  configure the Rating timestamp.
- src/NetflixClone.Infrastructure/Persistence/NetflixCloneDbContext.SaveChanges.cs:
  translate the specific competing-insert failure.
- src/NetflixClone.Web/NetflixClone.Web.Client/Program.cs: register API client.
- src/NetflixClone.Web/NetflixClone.Web.Client/Pages/MovieDetail.razor:
  include RatingControl.

No removed files, schema/generated EF changes, IUnitOfWork changes or new packages.
Existing untracked My List tests and video assets were left intact.
