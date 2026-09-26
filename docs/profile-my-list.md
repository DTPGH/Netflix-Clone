# Profile selection and My List MVP

## Boundaries

The validated JWT identifies the account. ProfileId is an explicit choice in the
route, not an identity claim. Every use case checks account ownership and
Profile.IsDeleted before reading or changing My List. Unknown, deleted and
another account's profile all return MyList.ProfileNotFound (404).

The browser's ActiveProfileState is a scoped WebAssembly service, held in memory
per tab. Reloading requires choosing again; no default profile is selected.
Logout/account changes clear selection. Token renewal keeps it. Selecting a
profile re-reads the active profile list through the API. Profile management
updates/removes the selected UI context. API errors invalidate deleted profiles.
The selected profile does NOT yet affect catalog visibility or playback age rules.

## API

All routes require Authorize and return Cache-Control: no-store.

- GET /api/profiles/{profileId}/my-list?page=1&pageSize=20
  returns items (movie summaries), page, pageSize, totalCount; maximum pageSize 50.
  Sort: CreatedAt descending, then association Id descending.
- GET /api/profiles/{profileId}/my-list/{movieId}
  returns { isInMyList }. Missing/deleted movie: 404.
- PUT /api/profiles/{profileId}/my-list/{movieId}: no body, 204.
- DELETE /api/profiles/{profileId}/my-list/{movieId}: no body, 204.

Adding requires a nondeleted movie; IsAvailable=false is allowed.
Listing omits deleted movies. Removing also works for a deleted movie and
physically deletes the association. Repeated add/remove succeeds without a write.
Mutations that need a write use one IUnitOfWork.SaveChangesAsync.

## Concurrency

The existing unique index prevents duplicate profile/movie pairs. The custom
DbContext override translates ONLY SQL Server 2601/2627 mentioning
UQ_MyListItems_ProfileId_MovieId, with one added MyListItem and no other pending
writes, to MyListAlreadyExistsException. Add treats it as an idempotent success.
Other persistence failures propagate. No EF exception is referenced by Application.
Concurrent delete of the same association is treated as success; this use case
only deletes one MyListItem, whose mapping has no concurrency properties.
There is no retry on a failed DbContext.

Simultaneous PUT/DELETE express competing intentions; final state follows
database interleaving, not browser click time. A later GET resolves current state.
Profile/movie soft-delete may race with an add after validation: an association
can remain stored, but deleted profiles are rejected on subsequent calls and
deleted movies are hidden from lists. This MVP adds no serializable locking
across profile/movie lifecycle operations.

Client components capture the selected profile and generation for each request,
discard late responses after profile/account changes, and never retarget an
in-flight mutation to a new profile. An uncertain mutation requires reloading
before another change. AuthSession coordinates token refresh; mutations are not
automatically retried.

## Validation

Run:
    dotnet build
    dotnet run --project tests/NetflixClone.MyList.Specs

The package-free executable checks use-case ownership, soft-delete, idempotency,
error propagation and paging. It does not replace SQL Server integration tests.

Manual/API integration checks:
1. Without login, the four endpoints return 401.
2. Create two profiles. Select A, save a movie, switch to B: its list is independent.
3. Foreign/unknown/deleted ProfileId returns identical 404 on all four routes.
4. Repeat PUT and DELETE; issue two PUTs simultaneously on separate API requests:
   both succeed and only one row exists. Repeat with DELETE.
5. Soft-delete a movie: list hides it, PUT/status return 404, DELETE still succeeds.
6. An unavailable movie can be saved and displays an unavailable label.
7. Fill more than 20 items; verify ordering, paging and removal on the last page.
8. Switch profile or logout while requests are pending: no old data is displayed.
9. Delete or rename the selected profile in Manage profiles; header updates.
10. Reload: choose profile again. No browser credentials are added by this feature.
11. Simulate a lost mutation response; reload status/list before changing it again.
12. An expired access token is refreshed through the existing AuthSession.

No schema, generated EF, IUnitOfWork or JWT changes. No rating, onboarding,
recommendations, watch history or Kids content filtering.

## Changed-file inventory

New files:
- `src/NetflixClone.Application/Common/Abstractions/Persistence/IMyListRepository.cs`
- `src/NetflixClone.Application/Common/Exceptions/MyListAlreadyExistsException.cs`
- `src/NetflixClone.Application/MyList/MyListModels.cs`
- `src/NetflixClone.Application/MyList/MyListErrors.cs`
- `src/NetflixClone.Application/MyList/ListMyListUseCase.cs`
- `src/NetflixClone.Application/MyList/GetMyListStatusUseCase.cs`
- `src/NetflixClone.Application/MyList/AddToMyListUseCase.cs`
- `src/NetflixClone.Application/MyList/RemoveFromMyListUseCase.cs`
- `src/NetflixClone.Infrastructure/Persistence/Repositories/MyListRepository.cs`
- `src/NetflixClone.Api/Contracts/MyList/MyListContracts.cs`
- `src/NetflixClone.Api/Controllers/MyListController.cs`
- `src/NetflixClone.Web/NetflixClone.Web.Client/Services/ActiveProfileState.cs`
- `src/NetflixClone.Web/NetflixClone.Web.Client/Services/MyListApiClient.cs`
- `src/NetflixClone.Web/NetflixClone.Web.Client/Pages/SelectProfile.razor`
- `src/NetflixClone.Web/NetflixClone.Web.Client/Pages/MyList.razor`
- `src/NetflixClone.Web/NetflixClone.Web.Client/Components/MyListButton.razor`
- `tests/NetflixClone.MyList.Specs/NetflixClone.MyList.Specs.csproj`
- `tests/NetflixClone.MyList.Specs/Program.cs`
- `docs/profile-my-list.md`

Modified files:
- `NetflixClone.slnx`: include the package-free use-case checks.
- `src/NetflixClone.Api/Program.cs`: register use cases.
- `src/NetflixClone.Infrastructure/DependencyInjection.cs`: register repository.
- `src/NetflixClone.Infrastructure/Persistence/NetflixCloneDbContext.SaveChanges.cs`: translate the specific duplicate association failure.
- `src/NetflixClone.Web/NetflixClone.Web.Client/Program.cs`: register client services.
- `src/NetflixClone.Web/NetflixClone.Web.Client/Components/AuthShell.razor`: selected-profile and My List navigation.
- `src/NetflixClone.Web/NetflixClone.Web.Client/Pages/MovieDetail.razor`: save/remove control.
- `src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Profiles.razor`: synchronize selection after management operations.
- `src/NetflixClone.Web/NetflixClone.Web/wwwroot/app.css`: profile choice interaction styles.

No files removed. Existing untracked video assets were not changed.
