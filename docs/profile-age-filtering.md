# Profile age filtering: Browse and My List

The authenticated client uses GET /api/profiles/{profileId}/movies. The API takes
the account from JWT sub, checks ownership and active profile status, and supplies
the database MaturityLevel to the catalog query. Client requests cannot set an age.
Unknown, foreign or deleted profiles return 404. The route requires Authorize and
no-store caching. Search, genre filtering, count and paging all operate after age
filtering (Movie.MinAge <= Profile.MaturityLevel). Unavailable movies remain visible,
matching the existing catalog/My List rules.

Anonymous browsing still uses the existing public /api/movies endpoint.
Authenticated users with no selected profile see a picker prompt instead of an
unfiltered list. Failed profile requests never fall back to public browsing.
Public catalog/detail metadata remain accessible: this is not parental control.

My List queries compare movie age with the current related profile in SQL before
counting/paging. Existing saved associations are not removed. Raising the age
setting makes hidden movies visible again, provided they are not deleted.
Add/status reject age-ineligible movies as the existing MovieNotFound response.
DELETE may still remove an association regardless of age or movie soft deletion.

Browse resets the page on profile changes, clears previous results and rejects
late responses. My List already does this. Editing a selected profile through
the existing management UI updates ActiveProfileState and reloads these pages.
Edits from another tab/device affect subsequent API reads, not already-rendered
pages via real-time push. A concurrent edit after the Browse profile read may
affect only the next request; no transaction locks were added.

## Verification

dotnet build and the My List executable specs (13 checks) pass. New checks cover
age restriction for add/status without deleting the saved row, current DB maturity
forwarding to Browse, and rejection of foreign/missing/deleted profiles.
No real SQL Server or browser integration test was run.

Manual: save a MinAge=18 movie in a standard profile, change it to Kids (13),
verify Browse/search and My List exclude it and totals/pages match. Change back
to standard: the saved item returns. Switch profiles during a delayed request.
Test forged profile IDs, no profile selected, and anonymous catalog behavior.

## Video demo proposal (not implemented)

Current Web Program serves /videos via public static-file middleware; Watch uses
that URL directly. Protecting the playback metadata POST alone does not protect
the MP4.

Recommended next slice:
1. Move demo media outside every wwwroot/static asset source. Remove public video
   middleware and old published copies/static mappings so the old URL returns 404.
2. Introduce an authorized profile-scoped playback-start endpoint. Validate account,
   profile ownership/deletion, current maturity, movie deletion/availability.
3. Return a short-lived, purpose-specific protected playback ticket, bound to
   account/profile/movie and expiry, rather than the raw public file URL. Never put
   access/refresh tokens in video URLs. Use a dedicated authentication scheme for
   tickets on the content endpoint; require that scheme with Authorize there.
4. Content requests validate the ticket and recheck current profile/movie eligibility.
   Resolve an allowlisted media identifier server-side; never accept a filesystem
   path. Stream with byte-range processing for seeking, not ReadAllBytes or a full
   browser Blob download. Use HTTPS and private/no-store caching.
5. Native video src requests do not inherit the Blazor HttpClient Bearer header.
   A restricted ticket URL supports the native player without changing the whole
   app to cookie authentication. Tickets are bearer credentials: redact query
   tokens from logs, use a no-referrer policy and short expiry. Shared URLs can work
   until expiry; this does not provide DRM or prevent copying received bytes.
6. Allow the same ticket for repeated Range requests until expiry; a one-use ticket
   breaks seeking. On expiry, obtain a new ticket with the normal authenticated
   client and restore playback position. Profile changes are checked on subsequent
   content requests; already buffered/in-flight bytes cannot be recalled.

For multiple instances, ticket signing/protection keys and private media need
shared/configured infrastructure. Logout/revocation semantics beyond ticket expiry
must be designed explicitly later. Full parental control also requires protecting
profile switching/editing and profile-aware detail/playback access.

References:
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-10.0
- https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/Range_requests
- https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/video

## Files changed in this slice

Added:
- src/NetflixClone.Api/Controllers/ProfileMoviesController.cs
- docs/profile-age-filtering.md

Modified:
- src/NetflixClone.Application/Catalog/Movies/BrowseMoviesQuery.cs
- src/NetflixClone.Application/Catalog/Movies/BrowseMoviesUseCase.cs
- src/NetflixClone.Application/Catalog/Movies/MovieCatalogCriteria.cs
- src/NetflixClone.Application/Common/Abstractions/Persistence/IMyListRepository.cs
- src/NetflixClone.Application/MyList/AddToMyListUseCase.cs
- src/NetflixClone.Application/MyList/GetMyListStatusUseCase.cs
- src/NetflixClone.Infrastructure/Persistence/Queries/MovieCatalogQueries.cs
- src/NetflixClone.Infrastructure/Persistence/Repositories/MyListRepository.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Services/CatalogApiClient.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Services/MyListApiClient.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Browse.razor
- src/NetflixClone.Web/NetflixClone.Web.Client/Pages/MyList.razor
- tests/NetflixClone.MyList.Specs/Program.cs (existing untracked specs extended).

No schema, generated EF, IUnitOfWork, video assets or media endpoint changes.
Existing uncommitted onboarding/recommendations work is preserved.
