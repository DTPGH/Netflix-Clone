# Onboarding and recommendations MVP

## Product flow

Login now navigates to /select-profile. Choosing an incomplete profile opens
/onboarding; completed profiles go to the validated return destination or Browse.
No profiles means the existing profile picker offers profile creation.

Onboarding offers 1–5 movie selections or Skip for now (an empty selection).
Selections persist across search/page changes until submission. Selected titles
can be removed from the selection summary. Completing or skipping saves backend
state and continues to /browse, /my-list, or /movies/{positive integer} when
requested. Arbitrary return URLs are rejected.

Active profile remains in memory per tab. Reloading requires selecting again;
OnboardingCompleted persists in the database and does not reset. Onboarding is
optional personalization, not an authorization requirement for rating/My List.
Choosing movies does not add ratings or My List items.

## API

Every endpoint requires Authorize, obtains account ID from validated JWT sub,
checks active profile ownership, and disables response caching.
Unknown, foreign and deleted profiles return the same 404.

- GET /api/profiles/{profileId}/onboarding:
  { onboardingCompleted, movieIds }.
- GET /api/profiles/{profileId}/onboarding/movies?page=1&pageSize=20&search=:
  paged MovieSummary items, page, pageSize, totalCount.
  Page >= 1, pageSize 1–50, trimmed search <= 100 characters.
- PUT /api/profiles/{profileId}/onboarding:
  { "movieIds": [1, 2] }, or [] to skip; returns saved onboarding state.
- GET /api/profiles/{profileId}/recommendations?limit=12:
  { items, source }; limit 1–24. Source is personalized, mixed or fallback.

GET candidates and PUT eligibility use the same conditions: nondeleted, available
movies with MinAge <= profile.MaturityLevel. Input IDs must be distinct, positive
and no more than five. Null/missing IDs are invalid. One ineligible movie rejects
the entire submission.

Completed onboarding accepts a retry with the same ID set, regardless of order;
different selections return 409. Stored IDs are compared before current movie
eligibility so a retry remains successful after a selected movie is removed.
Editing preferences is outside this slice.

## Persistence and concurrency

CompleteOnboardingUseCase adds preferences, sets OnboardingCompleted, and advances
Profile.UpdatedAt using IClock and existing ProfileRules.NextUpdatedAt. Exactly one
SaveChangesAsync commits these changes, including skip (profile update only).
The repository never saves. All persistence services use the same scoped context.

Existing Profile.IsDeleted/UpdatedAt concurrency tokens detect competing profile
changes. Existing ProfilePreferences unique constraint prevents duplicate pairs.
The custom SaveChanges override also translates the specific SQL Server
2601/2627 unique violation when the pending writes consist only of the onboarding
profile update and its new preferences. Batch exceptions may name multiple
entries. Application receives PersistenceConcurrencyException and returns 409;
there is no retry or global HTTP exception mapping. Other DB failures propagate.
EF retains its normal transaction rollback; a failed save must not partially
complete onboarding.

An incomplete profile with existing preferences returns a conflict rather than
silently repairing/overwriting inconsistent data.

Movie eligibility can change after validation; this MVP adds no cross-entity
locks. Recommendations recheck eligibility on each query. Recommendations and
fallback are separate reads, not a consistent database snapshot across concurrent
rating/catalog edits; the next refresh reflects current data.

## Recommendation rules

Application constants define weights: initial preference 1, Like 2, Love 3.
An explicit rating overrides the preference of the same movie; NotForMe
contributes no genre weight. Removing a rating allows its original preference to
contribute again. Sources must be nondeleted and age-appropriate; an unavailable
source can still express taste.

Infrastructure builds grouped genre weights and candidate scores in SQL using
the Movies.Genres skip navigation. It projects only MovieSummary and applies
Take before materialization; no full catalog is downloaded or loaded in memory.

Candidates must be nondeleted, available and age-appropriate. All movies already
chosen in onboarding or rated by the profile are excluded. My List is not used.
Order: score descending, release date descending, movie ID ascending.
If fewer than limit match, append distinct eligible candidates ordered by featured,
release date, then ID. Empty results are valid; age conditions are never relaxed.
Fallback is discovery content, not a claim of popularity or personalized ranking.
No recommendation rows, external service, cache, ML or background job is added.

## Client behavior

The onboarding page uses InteractiveWebAssembly with prerender disabled.
Recommendations renders inside the existing WebAssembly Browse page. Scoped
PersonalizationApiClient reuses AuthSession's coordinated refresh. Reads can
retry unauthorized through AuthSession; mutations are not automatically retried.

Components capture active-profile version/request generation and discard old
responses after switching profile, logging out, or disposal. A sent write may
still complete for its original profile. An uncertain write or 409 requires
Reload onboarding; if the prior commit succeeded, the UI shows Continue.

Browse suggestions have their own loading/error state and Refresh suggestions
button; failures do not disable the normal catalog. Returning to Browse after a
rating reloads suggestions. Existing public catalog/playback behavior is unchanged.
Age filtering here is not full parental control for Browse, Detail or playback.

## Validation

    dotnet build
    dotnet run --project tests/NetflixClone.Personalization.Specs --no-build

Package-free specs cover ownership, validation, single commit, skip, retries,
concurrency handling, failure propagation, query bounds and maturity forwarding.
A SQL Server provider probe confirms the ranking query translates before
ConnectionOpening, then deliberately aborts without database/network access.
This does not prove live SQL execution, ranking results or transactional races.

Manual tests:
1. Login -> picker; create a profile if needed; incomplete profile -> onboarding.
2. Select movies across pages/searches, remove from summary, enforce max 5.
3. Complete -> return destination; select the profile again -> no repeat onboarding.
4. Skip a new profile -> completed with no preferences -> fallback suggestions.
5. Two profiles have separate selections/ratings/suggestions.
6. Kids candidates and suggestions satisfy MinAge <= MaturityLevel, including
   after switching a standard profile to Kids.
7. Like/Love influence matching genres. NotForMe removes the initial preference
   contribution for that movie. Already selected/rated movies are not suggested.
8. Verify fallback fills remaining slots without duplicates, unavailable/deleted
   movies, or age-inappropriate movies; few/zero results remain usable.
9. Parallel completion requests: only one selection persists; conflicts return
   409 and no partial preferences. Same-set sequential retry succeeds.
10. Foreign/deleted profile calls return 404; anonymous calls return 401.
11. Lose a PUT response: reload recovers completed state if the save succeeded.
12. Switch profile/logout during requests: no old data or navigation is applied.
13. Invalid/external returnTo cannot navigate outside supported local destinations.
14. Failure of recommendations leaves search, genres and ordinary Browse usable.

No schema, generated EF, IUnitOfWork or auth contract changes. No preference
editing, watch history, rating aggregates, collaborative filtering or ML.

## Changed files

Added:
- src/NetflixClone.Application/Common/Abstractions/Persistence/IProfilePreferenceRepository.cs
- src/NetflixClone.Application/Common/Abstractions/Persistence/IPersonalizationQueries.cs
- src/NetflixClone.Application/Personalization/PersonalizationModels.cs
- src/NetflixClone.Application/Personalization/PersonalizationErrors.cs
- src/NetflixClone.Application/Personalization/GetOnboardingUseCase.cs
- src/NetflixClone.Application/Personalization/GetOnboardingMoviesUseCase.cs
- src/NetflixClone.Application/Personalization/CompleteOnboardingUseCase.cs
- src/NetflixClone.Application/Personalization/GetRecommendationsUseCase.cs
- src/NetflixClone.Infrastructure/Persistence/Repositories/ProfilePreferenceRepository.cs
- src/NetflixClone.Infrastructure/Persistence/Queries/PersonalizationQueries.cs
- src/NetflixClone.Api/Contracts/Personalization/PersonalizationContracts.cs
- src/NetflixClone.Api/Controllers/PersonalizationController.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Services/PersonalizationApiClient.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Services/ProfileNavigation.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Onboarding.razor
- src/NetflixClone.Web/NetflixClone.Web.Client/Components/Recommendations.razor
- tests/NetflixClone.Personalization.Specs/NetflixClone.Personalization.Specs.csproj
- tests/NetflixClone.Personalization.Specs/Program.cs
- docs/onboarding-recommendations.md

Modified:
- NetflixClone.slnx: include personalization checks.
- src/NetflixClone.Api/Program.cs: register use cases.
- src/NetflixClone.Infrastructure/DependencyInjection.cs: scoped repository/queries.
- src/NetflixClone.Infrastructure/Persistence/NetflixCloneDbContext.SaveChanges.cs:
  translate the specific onboarding duplicate constraint failure.
- src/NetflixClone.Web/NetflixClone.Web.Client/Program.cs: scoped API client.
- src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Login.razor: picker destination.
- src/NetflixClone.Web/NetflixClone.Web.Client/Pages/SelectProfile.razor: onboarding
  routing and updated descriptive text.
- src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Browse.razor: suggestions section.

No files removed. Existing untracked tests and videos were left intact.
