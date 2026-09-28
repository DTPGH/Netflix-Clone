# Profile detail and protected demo playback

## Runtime flow

Movie Detail now requires login and a selected profile. The client sends GET
/api/profiles/{profileId}/movies/{movieId} with the access token. Application checks
ownership, profile soft deletion and movie MinAge <= current MaturityLevel.
The old public GET /api/movies/{movieId} is removed. Public movie summaries remain
available from the existing public Browse endpoint; they contain no media URL.

Watch sends POST /api/profiles/{profileId}/movies/{movieId}/playback with its JWT.
Application checks the current account (exists, confirmed email, not manually
locked), profile ownership/deletion, movie deletion/age and availability.
The old POST /api/movies/{movieId}/playback route is removed.

The database VideoUrl is still the legacy /videos/filename.mp4 string. It is now
parsed only as a strict media identifier. No schema or seed migration is needed.
No remote URL, query string, traversal or client-supplied file path is accepted.

API verifies the private file exists, then issues an encrypted/authenticated
Data Protection ticket with account, profile, movie, media key and a 5-minute
expiry. Response: movieId, title, videoUrl (API-relative protected URL),
contentType, isDemo, expiresAtUtc. It contains no filesystem path.

The native player loads GET /api/media/{movieId}?ticket=... directly. This endpoint
requires the dedicated PlaybackTicket authentication scheme through Authorize.
A normal access JWT does not satisfy this scheme. Conversely, a playback ticket
does not authenticate other API routes. The ticket's movie must match the route.
Every GET/HEAD re-runs the current playback eligibility checks and media binding.
PhysicalFile with range processing returns partial content for seeking.
Missing/invalid/expired tickets return 401. Ineligible content returns 404.
Ticket issuance for an unavailable/missing file returns 409.

## Local file and deployment

The existing file was MOVED (not deleted) to:

    private-media/videos/bbb_sunflower_1080p_60fps_normal.mp4

This directory is ignored by Git. Keep a separate backup of this downloaded
asset. The default API root resolves ../../private-media/videos relative to the
API content root, which matches this repository layout during local development.
For deployment set DemoMedia:RootPath (environment variable DemoMedia__RootPath)
to an absolute private directory readable by the API process. Provision/copy the
media separately; it is deliberately not included in Web static assets.

Web excludes wwwroot/videos from static asset builds and explicitly returns 404
for /videos paths. The public video middleware has been removed. Rebuild and
restart running Web/API processes after this change. Old deployed servers or a
reverse proxy/CDN still exposing historical media must be cleaned/configured too.
The rebuilt static asset manifest no longer contains the demo file.

PrivateDemoMedia accepts filename-only MP4 keys and rejects file reparse points.
The media root is trusted server configuration and must not be exposed by another
static server, proxy alias or public storage bucket.

## Ticket keys, logging and limits

PlaybackTicketService implements a framework-independent Application abstraction.
Infrastructure uses ASP.NET Core Data Protection with the application name
NetflixClone.Playback and purpose NetflixClone.PlaybackTicket.v1. No JWT signing
key or new hardcoded secret is introduced. Infrastructure adds a shared framework
reference, not a new NuGet package.

Data Protection uses its host-specific default key storage. Production must
configure persistent protected key storage; multiple instances must share the
key ring and application name. Losing keys invalidates old tickets. Key material
must never be committed or placed under wwwroot.

Ticket URLs are bearer credentials. The API disables Hosting.Diagnostics request
URL logging at Information level because it includes query strings. No code logs
ticket bodies or URLs. Responses are private/no-store and the Web document uses
no-referrer. Operators must also redact query strings in proxy/IIS/APM/access logs
and avoid request/response-body logging for issuance/content.

The browser necessarily holds the ticket in runtime memory/video src and can see
it in network tools. Nothing persists it to browser storage or prerendered HTML.
Access/refresh tokens are never embedded in video URLs. Tickets can be reused
for Range requests until expiry, so copying one can allow playback until expiry.
Logout/device revocation does not immediately revoke issued tickets in this slice.
Account lock/profile deletion/age changes are enforced on subsequent requests.
Already buffered bytes or an existing long-running response cannot be recalled.
This is access control for a demo, not DRM or complete parental control: profile
switch/edit protection, subscriptions and stream limits remain separate work.

## Client lifecycle

Detail discards old responses and clears trailers when profile context changes.
Watch stops and removes the video source on profile change, logout or disposal;
it requires starting playback again for the new profile.

demo-player.js renews access shortly before ticket expiry and checks expiry on
returning to a visible tab. One renewal runs at a time. The new source restores
currentTime and the previous playing/paused state (autoplay policy may still
require user interaction). Renewals use the existing AuthSession coordinator for
access-token refresh. Failures stop playback and show a safe Retry message.
Generation checks prevent late responses from restoring an old profile's source.
A per-component JS ID retains cleanup access to detached video elements.

The player downloads via normal streaming/range requests, not a full MP4 Blob
or ReadAllBytes allocation. Refreshing tickets can still cause a brief rebuffer.

## Validation

    dotnet build
    dotnet build tests/NetflixClone.Playback.Specs
    dotnet run --project tests/NetflixClone.Playback.Specs --no-build
    node tests/NetflixClone.Playback.Specs/player-checks.mjs

28 HTTP checks run a real local Kestrel/controller/auth/file response pipeline with
fake repository data, ephemeral keys and a 256-byte test asset. They cover missing
credentials, removed routes, scheme separation, issuance, HEAD, byte-range 206
and exact bytes, expiry/tampering, ownership, age changes, locked account,
profile deletion, unavailable media and traversal rejection.
JS checks use a simulated video element for renewal deduplication, position/resume
and cleanup. These do not verify SQL Server queries, real MP4 decoding or browser
autoplay/seek behavior.

Manual browser tests:
1. Direct legacy /videos/... URL returns 404; old detail/playback routes fail.
2. Anonymous Detail prompts login; logged-in users must select a profile.
3. Kids cannot load an over-age Detail or start playback via a copied URL.
4. Standard profile can play/toggle pause/seek/volume/fullscreen.
5. Leave video playing across 5 minutes; renewal preserves position and state.
6. Leave tab hidden past expiry and return; access renews or safely shows Retry.
7. Logout/switch profile while a ticket request is delayed: old video stops.
8. Change profile age or availability on the server; later content requests fail.
9. Test missing MP4, tampered/expired ticket and failed network without exposing
   raw tickets in UI/error text.
10. Confirm real deployment logs do not record ticket query strings.

## Files changed

Added:
- src/NetflixClone.Application/Catalog/Movies/ProfileMovieQuery.cs
- src/NetflixClone.Application/Common/Abstractions/Security/IPlaybackTicketService.cs
- src/NetflixClone.Infrastructure/Security/PlaybackTicketService.cs
- src/NetflixClone.Api/Security/PlaybackTicketHandler.cs
- src/NetflixClone.Api/Media/PrivateDemoMedia.cs
- src/NetflixClone.Api/Controllers/MediaController.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/wwwroot/js/demo-player.js
- tests/NetflixClone.Playback.Specs/NetflixClone.Playback.Specs.csproj
- tests/NetflixClone.Playback.Specs/Program.cs
- tests/NetflixClone.Playback.Specs/player-checks.mjs
- docs/protected-demo-playback.md

Modified:
- .gitignore: ignore private media.
- src/NetflixClone.Application/Catalog/Movies/GetMovieDetailUseCase.cs
- src/NetflixClone.Application/Catalog/Movies/IGetMovieDetailUseCase.cs
- src/NetflixClone.Application/Catalog/Movies/GetMoviePlaybackUseCase.cs
- src/NetflixClone.Application/Catalog/Movies/MoviePlayback.cs
- src/NetflixClone.Infrastructure/Persistence/Queries/MovieCatalogQueries.cs
- src/NetflixClone.Infrastructure/NetflixClone.Infrastructure.csproj
- src/NetflixClone.Api/Contracts/Catalog/PlaybackResponse.cs
- src/NetflixClone.Api/Controllers/MoviesController.cs
- src/NetflixClone.Api/Controllers/ProfileMoviesController.cs
- src/NetflixClone.Api/Controllers/PlaybackController.cs
- src/NetflixClone.Api/Program.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Models/CatalogModels.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Services/CatalogApiClient.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Services/CatalogMedia.cs
- src/NetflixClone.Web/NetflixClone.Web.Client/Pages/MovieDetail.razor
- src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Watch.razor
- src/NetflixClone.Web/NetflixClone.Web/Program.cs
- src/NetflixClone.Web/NetflixClone.Web/NetflixClone.Web.csproj
- src/NetflixClone.Web/NetflixClone.Web/Components/App.razor

Moved: the existing untracked MP4 from Web/wwwroot/videos to private-media/videos.
No database writes, schema/generated EF/IUnitOfWork changes or commits.
