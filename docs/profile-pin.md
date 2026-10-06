# Optional profile PIN MVP

Profiles may have no PIN, or a PIN containing exactly four ASCII digits. PINs are
strings so `0123` is valid. Only BCrypt hashes are stored in `Profiles.PinHash`.
Neither the PIN, its hash, account passwords nor unlock tokens appear in profile
responses, logs or browser persistent storage. `HasPin` is the only PIN metadata
returned by profile listing and mutation responses.

## API contracts

All endpoints require the existing validated account JWT. Account identity comes
from `sub`, never request bodies.

| Endpoint | JSON request | Response |
|---|---|---|
| PUT `/api/profiles/{profileId}/pin` | `accountPassword`, `pin` | Safe profile response, including `hasPin` |
| DELETE `/api/profiles/{profileId}/pin` | `accountPassword` | Safe profile response, including `hasPin` |
| POST `/api/profiles/{profileId}/unlock` | `pin` (nullable for an unprotected profile) | `unlockToken`, `expiresAtUtc` |

Create and Update profile now also require `accountPassword` in their existing
JSON requests. Delete profile now requires a JSON body containing
`accountPassword`. Management deliberately uses account-password verification,
not an old PIN: the account owner can recover a forgotten PIN, and someone with
only a profile PIN cannot remove protection, change maturity or create another
adult profile. Passwords are preserved exactly, used for verification and cleared
from the UI after requests; they are not persisted by the client.

Unknown, deleted and other-account profiles return the same 404 for unlock,
PIN management and content access. Existing idempotent Delete behavior for an
already-deleted owned profile is retained, with account-password verification.

## Unlock attempts and persistence

Defaults live in `ProfilePinRules`: five incorrect attempts lock PIN verification
for five minutes. An already locked profile does not verify or increment. At
expiry, reset the old state before processing the next attempt; correct PIN
verification resets both fields. Changing or removing the PIN also resets them.
Invalid formats are rejected without treating them as credential attempts.

`ProfilePinUseCase` reuses `IAuthenticationMutationScopeFactory`. Infrastructure
opens an explicit transaction and obtains the account row's `UPDLOCK,HOLDLOCK`
before reading the profile. The account lock remains held through attempt/reset
mutation, one `IUnitOfWork.SaveChangesAsync` and commit. Requests from multiple API
instances therefore cannot lose increments. No in-memory lock is used in
production. Account-password profile management follows the same account locking
order; Create retains the existing profile-creation scope and five-profile limit.

`PinHash`, `PinFailedAttempts` and `PinLockoutEnd` are additional concurrency
tokens in the custom partial mapping, alongside `IsDeleted` and `UpdatedAt`.
PIN management advances `UpdatedAt`; attempt counters do not invalidate grants
or update unrelated profile details. A persistence concurrency conflict returns
`Profiles.ConcurrentChange` (409), with transaction disposal preserving rollback.
Other database failures are not disguised as wrong PINs. Unlock tokens are issued
only after a successful commit. `IUnitOfWork` and generated EF files are unchanged.

## Unlock proof and API guard

Infrastructure uses the existing ASP.NET Core Data Protection provider with a
separate purpose, `NetflixClone.ProfileUnlock.v1`. The protected payload contains
account ID, profile ID, expiry, a unique nonce and a fingerprint of the salted PIN
and account password hashes. Application sees only a framework-independent token
service. Lifetime is 30 minutes, checked without extra clock skew. A token is bound
to its account/profile and invalidated by changing the PIN or account password.
The fingerprint is not a hash of the four raw digits and cannot be used to test
all 10,000 PINs offline without obtaining the database hashes first.

`RequireProfileAccessAttribute` is the API adapter for the Application
`IProfileAccessGuard`. Existing `[Authorize]` validates the account JWT first;
the filter then checks ownership, live account/profile eligibility and, when a
PIN is configured, `X-Profile-Unlock`. Missing, invalid or expired proof returns
403 `Profiles.UnlockRequired` before the content action executes. Guards cover
profile browse/detail, collections, My List, ratings, onboarding, recommendations,
watch history/continue watching, viewing sessions and playback issuance.

Profile listing stays available for choosing/managing profiles. Management
requires the account password instead of an unlock token. Public catalog and
Admin APIs remain outside this profile authorization policy. The guard is an
HTTP boundary check: direct internal callers of the existing content use cases
do not automatically acquire this check.

The protected playback ticket carries the unlock proof internally. Media GET/HEAD
requests recheck it against current profile/PIN/account state before serving MP4
bytes, because a video element does not send the client API handler's headers.
Existing movie maturity, subscription, ownership and media-path checks remain.
Already-open streams and bytes the browser has downloaded cannot be recalled;
new range requests/tickets are checked again.

## Browser flow

Manage profiles provides account-password-confirmed PIN set/change/remove forms.
Choose profile shows a PIN-protected badge and a password-style numeric input
when required. A profile without a PIN follows the same unlock endpoint with a
null PIN, without an extra UI prompt.

`ActiveProfileState` selects a profile only after successful unlock, with checks
against session changes while awaiting requests. `ProfileAccessState` keeps the
proof in tab-local memory. Logout, account changes, explicit selection clearing,
and PIN changes from the local management page clear it. Reload requires choosing
the profile again. Normal account access-token refresh does not discard the grant.

`ProfileAccessHandler` attaches proof only to matching-profile content requests
to the configured HTTPS API origin. It does not send proof to other origins or
profile management/unlock endpoints. The shared handler also covers JWT retries.
An unlock-required response clears only the grant version associated with the
request and navigates to profile selection. A late response cannot clear a newer
grant. The error is separate from account 401 and subscription 403, so it does not
start account refresh loops or show a subscription-required message.

Swagger documents the optional `X-Profile-Unlock` header; API CORS explicitly
allows that header for the configured Web origins. Swagger is documentation,
not the authorization mechanism.

## Limits to understand

- This is profile protection within an account, not a replacement for account
  authentication. Someone knowing the account password can manage all profiles.
- Unlock proof is a bearer secret bound to account/profile and configuration,
  not a particular device or refresh session. Do not claim session binding: the
  current account JWT has no stable session identifier. Keep the proof out of
  localStorage, logs, debugging output and URLs (apart from its encrypted nesting
  inside the existing protected playback ticket).
- Multi-instance deployments must share the Data Protection key ring, application
  name and reliable UTC time, as they already must for playback tickets.
- Five-minute lockout slows online guessing but four digits are intrinsically
  weak. Anyone with access to database BCrypt hashes can attempt offline guessing.
  Account-password management requests retain the current password-check behavior;
  this feature adds no separate account-password attempt limiter or rate limiting.
- PIN unlock lockout does not revoke grants that were already successfully issued.
- No new table or column beyond the already-applied/scaffolded PIN attempt fields
  is introduced by this implementation.

## Verification

Run the standalone, dependency-free executable checks:

```powershell
dotnet run --project tests/NetflixClone.ProfilePin.Specs/NetflixClone.ProfilePin.Specs.csproj
```

They cover PIN format/leading zeros, wrong attempts, threshold/expiry/reset,
commit conflicts, password-confirmed management, proof tampering/ownership/
expiry/PIN and password changes, protected playback payload, filter action
blocking, controller coverage, custom EF concurrency metadata, and late client
grant invalidation. They use fake persistence and a real BCrypt/Data Protection
implementation; they do not prove SQL locking behavior with a live database.

Manual browser/SQL checks:

1. Use an unprotected profile; Browse, My List and playback still work.
2. Set `0123` with account password; choose the profile and enter that PIN.
3. Enter five wrong PINs; verify both persisted fields, no sixth increment and
   rejection of even the correct PIN while locked. Wait five minutes and retry.
4. Send simultaneous wrong unlock requests against the actual database; the count
   must reach five, then remain there until expiry.
5. Call protected APIs with only the account JWT, a wrong-profile proof or expired
   proof; expect 403. Other-account profile IDs still return 404.
6. Change/remove PIN using the account password; verify reset state and that old
   proofs cannot access a still-protected profile. Changing the account password
   also invalidates previous proof.
7. Switch profile/logout during pending requests; late responses must not restore
   old selection or erase a newly obtained grant.
8. Try a media ticket issued before changing PIN; subsequent media requests must
   reject it. Confirm ordinary ticket renewal, subscription and maturity checks.
9. Confirm profile Create/Update/Delete require the account password and retain
   the maximum-five-profile rule.

## Files changed in this implementation

The generated Profile and DbContext files were supplied by the preceding scaffold and are not edited by this implementation.

- [docs/profile-pin.md](D:/Cybersoft/NetflixClone/docs/profile-pin.md)
- [src/NetflixClone.Api/Contracts/Profiles/CreateProfileRequest.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Contracts/Profiles/CreateProfileRequest.cs)
- [src/NetflixClone.Api/Contracts/Profiles/ProfilePinRequests.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Contracts/Profiles/ProfilePinRequests.cs)
- [src/NetflixClone.Api/Contracts/Profiles/ProfileResponse.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Contracts/Profiles/ProfileResponse.cs)
- [src/NetflixClone.Api/Contracts/Profiles/UpdateProfileRequest.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Contracts/Profiles/UpdateProfileRequest.cs)
- [src/NetflixClone.Api/Controllers/MediaController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/MediaController.cs)
- [src/NetflixClone.Api/Controllers/MovieCollectionsController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/MovieCollectionsController.cs)
- [src/NetflixClone.Api/Controllers/MyListController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/MyListController.cs)
- [src/NetflixClone.Api/Controllers/PersonalizationController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/PersonalizationController.cs)
- [src/NetflixClone.Api/Controllers/PlaybackController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/PlaybackController.cs)
- [src/NetflixClone.Api/Controllers/ProfileMoviesController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/ProfileMoviesController.cs)
- [src/NetflixClone.Api/Controllers/ProfilesController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/ProfilesController.cs)
- [src/NetflixClone.Api/Controllers/RatingsController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/RatingsController.cs)
- [src/NetflixClone.Api/Controllers/ViewingSessionsController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/ViewingSessionsController.cs)
- [src/NetflixClone.Api/Controllers/WatchHistoryController.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Controllers/WatchHistoryController.cs)
- [src/NetflixClone.Api/OpenApi/AuthorizeOperationFilter.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/OpenApi/AuthorizeOperationFilter.cs)
- [src/NetflixClone.Api/Program.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Program.cs)
- [src/NetflixClone.Api/Security/PlaybackTicketHandler.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Security/PlaybackTicketHandler.cs)
- [src/NetflixClone.Api/Security/RequireProfileAccessAttribute.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Api/Security/RequireProfileAccessAttribute.cs)
- [src/NetflixClone.Application/Common/Abstractions/Security/IPlaybackTicketService.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Common/Abstractions/Security/IPlaybackTicketService.cs)
- [src/NetflixClone.Application/Common/Abstractions/Security/IProfileUnlockTokenService.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Common/Abstractions/Security/IProfileUnlockTokenService.cs)
- [src/NetflixClone.Application/Profiles/CreateProfileCommand.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/CreateProfileCommand.cs)
- [src/NetflixClone.Application/Profiles/CreateProfileUseCase.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/CreateProfileUseCase.cs)
- [src/NetflixClone.Application/Profiles/DeleteProfileCommand.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/DeleteProfileCommand.cs)
- [src/NetflixClone.Application/Profiles/DeleteProfileUseCase.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/DeleteProfileUseCase.cs)
- [src/NetflixClone.Application/Profiles/IProfileAccessGuard.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/IProfileAccessGuard.cs)
- [src/NetflixClone.Application/Profiles/IProfilePinUseCase.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/IProfilePinUseCase.cs)
- [src/NetflixClone.Application/Profiles/ProfileAccessGuard.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/ProfileAccessGuard.cs)
- [src/NetflixClone.Application/Profiles/ProfileAccountPasswordVerifier.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/ProfileAccountPasswordVerifier.cs)
- [src/NetflixClone.Application/Profiles/ProfileErrors.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/ProfileErrors.cs)
- [src/NetflixClone.Application/Profiles/ProfilePinRules.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/ProfilePinRules.cs)
- [src/NetflixClone.Application/Profiles/ProfilePinUseCase.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/ProfilePinUseCase.cs)
- [src/NetflixClone.Application/Profiles/ProfileSummary.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/ProfileSummary.cs)
- [src/NetflixClone.Application/Profiles/UpdateProfileCommand.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/UpdateProfileCommand.cs)
- [src/NetflixClone.Application/Profiles/UpdateProfileUseCase.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Application/Profiles/UpdateProfileUseCase.cs)
- [src/NetflixClone.Infrastructure/Persistence/NetflixCloneDbContext.Concurrency.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Infrastructure/Persistence/NetflixCloneDbContext.Concurrency.cs)
- [src/NetflixClone.Infrastructure/Persistence/Repositories/ProfileRepository.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Infrastructure/Persistence/Repositories/ProfileRepository.cs)
- [src/NetflixClone.Infrastructure/Security/PlaybackTicketService.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Infrastructure/Security/PlaybackTicketService.cs)
- [src/NetflixClone.Infrastructure/Security/ProfileUnlockTokenService.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Infrastructure/Security/ProfileUnlockTokenService.cs)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Components/ProfileForm.razor](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Components/ProfileForm.razor)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Models/ProfileModels.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Models/ProfileModels.cs)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Profiles.razor](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Profiles.razor)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Pages/SelectProfile.razor](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Pages/SelectProfile.razor)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Watch.razor](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Pages/Watch.razor)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Program.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Program.cs)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Services/ActiveProfileState.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Services/ActiveProfileState.cs)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Services/CatalogApiClient.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Services/CatalogApiClient.cs)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Services/ProfileAccessHandler.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Services/ProfileAccessHandler.cs)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Services/ProfileAccessState.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Services/ProfileAccessState.cs)
- [src/NetflixClone.Web/NetflixClone.Web.Client/Services/ProfilesApiClient.cs](D:/Cybersoft/NetflixClone/src/NetflixClone.Web/NetflixClone.Web.Client/Services/ProfilesApiClient.cs)
- [tests/NetflixClone.Playback.Specs/Program.cs](D:/Cybersoft/NetflixClone/tests/NetflixClone.Playback.Specs/Program.cs)
- [tests/NetflixClone.Playback.Specs/ViewingSessionChecks.cs](D:/Cybersoft/NetflixClone/tests/NetflixClone.Playback.Specs/ViewingSessionChecks.cs)
- [tests/NetflixClone.ProfilePin.Specs/NetflixClone.ProfilePin.Specs.csproj](D:/Cybersoft/NetflixClone/tests/NetflixClone.ProfilePin.Specs/NetflixClone.ProfilePin.Specs.csproj)
- [tests/NetflixClone.ProfilePin.Specs/Program.cs](D:/Cybersoft/NetflixClone/tests/NetflixClone.ProfilePin.Specs/Program.cs)
