# Account-wide session revocation

Password Reset/Change revoke all unrevoked Devices and RefreshTokens for that account as part of the same save as the password update/reset-token consumption. Wrong old password, invalid reset link or ineligible account performs no revocation. Previously revoked timestamps are preserved. Expired but unrevoked tokens are also marked revoked. Profiles/subscriptions/history are not changed.

POST /api/auth/devices/revoke-all requires Authorize; account ID comes only from the validated sub claim, never a request body. Success returns 204, ineligible account 403, persistence concurrency conflict 409. Repeating the operation is safe and returns 204. This includes the current browser, not only remote devices. The UI asks for confirmation on /devices and navigates to /login after success. Anyone with the current password can explicitly log in again.

## Database coordination

IAuthenticationMutationScopeFactory is a small Application abstraction, not an EF transaction type. Infrastructure begins an explicit SQL Server transaction, then obtains UPDLOCK/HOLDLOCK on UserAccounts by account ID or normalized email using parameterized SQL. Login reads the account only after locking. Refresh first looks up only the account ID from TokenHash with scalar SQL, acquires the account lock, then loads the tracked token/account/device and checks their current state. Password operations, global revoke and single-device revoke use the same lock.

The row update lock is held until commit/disposal and serializes these operations across API instances. The factory, repositories and IUnitOfWork share the same scoped DbContext. No in-memory production locks, schema changes or generated EF changes. Repositories stage changes; a use case saves once and commits its scope. Scope disposal without commit rolls back; database/deadlock/timeout errors are not converted to successful revocation. Existing token/device optimistic concurrency checks remain enabled. Refresh persistence conflicts remain InvalidRefreshToken; revoke conflicts remain 409. No automatic mutation retries.

- Rotation first: replacement commits, then global revoke loads and revokes it.
- Global revoke first: queued rotation reloads the revoked token/device and rejects it without inserting a replacement.
- Password change first: queued Login verifies against the new password hash. An old-password login fails; a new explicit login with the new password can create a fresh device.
- Login first: its issued device/token commits, then revoke includes it.
- Viewing checkpoint/start takes the account lock before the device lock, keeping account -> device ordering consistent with revocation. This serializes those short transactions with auth mutations; it is a simplicity/correctness trade-off for this project.

Existing logout remains scoped to the submitted refresh token and retains its concurrency semantics. This feature does not turn logout into global revocation.

## Client coordination and limits

AuthSession.RevokeSessionsAsync pauses its timer and invalidates in-flight publications, drains an existing rotation by taking the browser credential-store lease, and holds that lease through the API action and cleanup. On success it changes the cross-tab epoch, clears persistent credentials and in-memory authentication state. A late refresh response cannot publish credentials after this action. A known business rejection retains the session and resumes scheduling. An unknown/lost response or server error clears the local session conservatively and asks the user to sign in; no automatic resend. Storage failure is reported instead of claiming persistent credentials were cleared.

Other physical devices discover revocation when attempting refresh. Previously issued access JWTs (15-minute lifetime plus configured validation skew) and playback tickets are NOT blacklisted; existing access/buffered video can continue until expiry. Immediate access-token invalidation is a separate feature. A valid older JWT can still call authenticated endpoints during that window. This is explicitly disclosed in the device confirmation and password UI.

## Verification

Build solution and run Playback.Specs. New checks cover ownership, password/reset single-save revocation, idempotency, rollback, propagation of database failures, both rotation/revoke orders, queued old-password login rejection and new-device creation after an explicit fresh login. HTTP checks verify anonymous denied, User allowed, 204/repeat and no-store. Concurrency tests use serialized fake transactional scopes; they do not prove SQL Server locking or real browser timing.

Manual SQL/browser tests: sign into two browsers; change/reset password and try refreshing both; sign out all from Devices; sign in again and confirm a new device identifier is issued; race refresh with revoke-all/password change on multiple API instances; verify no active replacement survives after the revoke transaction; inject save failure and verify password/token/device rollback; sign out during a pending client request; test another open tab and a lost API response. Existing access can still work briefly as described above.

Files added: Application/Common/Abstractions/Persistence/IAuthenticationMutationScopeFactory.cs; Application/Authentication/Devices/RevokeAllDevicesUseCase.cs (also contains the staging helper); Infrastructure/Persistence/AuthenticationMutationScopeFactory.cs; Api/Controllers/AccountSessionsController.cs; tests/NetflixClone.Playback.Specs/AuthenticationMutationTestData.cs and SessionRevocationChecks.cs; this document.

Files modified for this extension: Application/Authentication/Passwords/PasswordUseCase.cs, Authentication/Login/LoginUseCase.cs, Authentication/Refresh/RefreshTokenUseCase.cs, Authentication/Devices/RevokeDeviceUseCase.cs, Common/Abstractions/Persistence/IDeviceRepository.cs and IRefreshTokenRepository.cs; Infrastructure/Persistence/Repositories/DeviceRepository.cs and RefreshTokenRepository.cs, Persistence/ViewingSessionScopeFactory.cs, DependencyInjection.cs; Api/Program.cs; Web.Client/Services/AuthSession.cs and DevicesApiClient.cs, Pages/Password.razor and Devices.razor; tests/NetflixClone.Playback.Specs/PasswordChecks.cs and Program.cs; docs/password-management.md. Prior password feature files remain uncommitted.
