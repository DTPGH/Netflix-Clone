# Frontend authentication

## Run locally

Use two terminals from the repository root:

```powershell
dotnet run --project src/NetflixClone.Api --launch-profile https
dotnet run --project src/NetflixClone.Web/NetflixClone.Web --launch-profile https
```

Open https://localhost:7012. The API is https://localhost:7248. Trust the local HTTPS development certificate if needed (`dotnet dev-certs https --trust`). The API still requires its existing database and JWT User Secrets. Never copy these into Web or Client settings.

Client `wwwroot/appsettings.Development.json` contains the public API URL. API `appsettings.Development.json` permits only the two exact Web development origins. For production set the published Client's `Api:BaseUrl` and API `Cors:AllowedOrigins` to the deployed HTTPS origins. The default production URL is deliberately empty. No wildcard origins or credentialed CORS are enabled.

## Boundaries and rendering

Web references Client only. Client talks to API through HTTP and owns separate DTOs; neither frontend project references Application or Infrastructure.

Home/account, Login, Register and Confirm Email are Client components with `InteractiveWebAssembly` and `prerender: false`. AuthShell runs within those client roots. Browser storage/auth services are scoped and registered only in Client. Web keeps its Interactive Auto/Server capabilities for future pages. No token is serialized into prerendered HTML or persistent component state.

Bootstrap is retained. `app.css` defines a small dark palette and radius tokens; layout/spacing uses Bootstrap utilities. Counter, Weather, Home demo and sidebar navigation were removed. Error, NotFound and reconnection infrastructure remain.

## Session flow

- Register sends Email/Password/ConfirmPassword. Password fields are cleared after submission and never persisted.
- Confirmation requires account ID and token. Development currently uses the API's development email sender: retrieve the code from its local terminal. The UI accepts the code masked; it does not put it in a URL or storage. Resend displays a generic response.
- Login supplies DeviceType=Web, the user's friendly DeviceName and any server-issued stored DeviceIdentifier. IP/User-Agent come from the browser request, not an intermediary server. There is no fingerprinting.
- Save the refresh credential and device identifier, then call `/me` with the access token. Only a successful `/me` constructs the UI ClaimsPrincipal. JWT decoding is not used as proof of identity.
- The AuthenticationStateProvider and AuthSession are the same scoped instance. Notifications update AuthorizeView/navigation. API `[Authorize]` remains the authority; client visibility is not access control.
- Access tokens exist only in memory. A hard reload restores by rotating the stored refresh token and checking `/me` again.
- Refresh is on demand when checking a session with an absent/nearly expired access token, or after a `/me` 401. No background timer is installed. Refresh/verification tasks are shared within the tab. A browser Web Lock serializes Login/refresh/logout across tabs; refresh reads storage only after acquiring the lock, so another tab's replacement is respected. A current secure-context browser with Web Locks is required; there is no unsafe fallback.
- Rotation persists the returned refresh token and its unchanged absolute expiration. No automatic retry of a failed/ambiguous rotation. If the response is lost, the user may need to log in again.
- Logout immediately invalidates in-flight UI updates and clears in-memory identity, signals other tabs, waits for pending credential work, then calls backend Logout with the latest stored token. It clears stored refresh credentials even if the API fails. DeviceIdentifier remains for browser recognition. A late refresh may save its replacement while holding the lock, but cannot authenticate the UI; the waiting logout then revokes/clears that replacement. Other tabs invalidate their in-memory identities when the session epoch changes.
- Backend logout still revokes only the submitted token. No claim of revoking all device sessions or already-issued access JWTs is made. A network failure or tab termination can prevent confirmed server revocation; UI reports failure to confirm.

## Browser storage trade-off

`ICredentialStore` hides localStorage from UI. `BrowserCredentialStore` delegates to a small JS module. Only the refresh token + expiry, device identifier and a non-secret session epoch are persisted. Access tokens/passwords are not stored. Nothing logs raw tokens, identifiers or API response bodies.

localStorage is browser-accessible, not inherently secure: XSS or malicious same-origin script can steal a refresh token. This implements the current JSON API, not a production claim of HttpOnly protection. Migrating to HttpOnly cookies will require backend transport/CSRF decisions and replacing the storage/session adapter; auth forms need not understand those details. Do not add third-party scripts casually. Production needs appropriate CSP and the wider security review outside this slice.

## Manual browser checks

The repeatable browser checks are in `tests/frontend/auth.browser.cjs`. With the Web host running and Node/Playwright available, run `node tests/frontend/auth.browser.cjs`. The script uses installed Edge by default (`PLAYWRIGHT_CHANNEL=chrome` selects Chrome), mocks API responses and writes screenshots to the OS temporary directory (or `AUTH_SCREENSHOT_DIR`). It does not test database persistence. No Playwright runtime is added to production projects.

This implementation was checked with a clean solution build, desktop/mobile browser flows, concurrent two-tab restoration, logout during an in-flight refresh, offline logout, and blocked storage. API preflight was separately checked against the running API for an allowed and unrelated origin. Real account/SQL integration remains a manual check.

1. Desktop and narrow mobile viewport: readable contrast, no horizontal scroll, keyboard-visible focus, labels and validation announced; tab through every form.
2. Register with invalid email, short/mismatched passwords; then create a test account. Duplicate email shows a friendly message. Password fields clear after submitting.
3. Confirm using its account number/code; invalid code fails safely. Resend for unknown/confirmed emails remains generic.
4. Login with wrong password/unconfirmed/locked account; ensure no authenticated navigation. Login with a confirmed account invokes `/me`, then shows ID/roles.
5. Repeat Login with the same browser: request uses the saved DeviceIdentifier. DeviceName is editable, DeviceType is Web.
6. Reload the signed-in page: one restoration flow rotates the refresh token and verifies `/me`. Inspect Network only in a trusted local development session; never share credential-bearing screenshots.
7. Check session near access expiry; concurrent calls await the same work. Open two tabs and reload: refresh calls serialize and read the newest stored credential, without reusing the same token.
8. Logout with a throttled refresh in flight: no late response restores the UI; credentials are cleared, device identifier retained. Other tabs become anonymous. Backend logout may not invalidate already-issued JWTs.
9. Simulate API offline during Logout: browser becomes anonymous and displays the server-revocation warning. Simulate lost refresh response: no retry loop.
10. Block site storage: Login is disabled with an actionable message, while Register/confirmation still work. Clear/corrupt stored credentials and reload: no secret appears in an error.
11. CORS preflight from the configured origin succeeds; unrelated origins do not receive an allow-origin response. Requests must go directly to API, not Web server.

## Study points

Read AuthApiClient (contracts/errors), ICredentialStore + JS module (storage and locks), then AuthSession (generation/epoch guards and the commit-like order of storage + `/me` + UI publication). AuthShell observes state; it does not own credentials. The original backend token lifecycle is unchanged.

## File summary for this implementation

New Client files:
- `Models/AuthModels.cs`
- `Services/AuthApiClient.cs`, `Services/AuthSession.cs`
- `Services/ICredentialStore.cs`, `Services/BrowserCredentialStore.cs`
- `wwwroot/js/auth-storage.js`
- `Components/AuthShell.razor`
- `Pages/Home.razor`, `Pages/Login.razor`, `Pages/Register.razor`, `Pages/ConfirmEmail.razor`

Modified Client template files: `Program.cs`, `_Imports.razor`, `NetflixClone.Web.Client.csproj`, `wwwroot/appsettings.json`, `wwwroot/appsettings.Development.json`. The only new production package is Microsoft's `Microsoft.AspNetCore.Components.Authorization` 10.0.10, matching the installed Blazor version.

Modified Web template files: `Program.cs`, `Components/App.razor`, `Components/Layout/MainLayout.razor`, `wwwroot/app.css`.

Removed template files: Client `Pages/Counter.razor`; Web `Components/Pages/Home.razor`, `Components/Pages/Weather.razor`, `Components/Layout/NavMenu.razor`, `Components/Layout/NavMenu.razor.css`, `Components/Layout/MainLayout.razor.css`.

Backend modifications are limited to API `Program.cs` and `appsettings.Development.json` for CORS. New supporting files are this document and `tests/frontend/auth.browser.cjs`. Existing user changes adding the frontend projects to `NetflixClone.slnx` are retained.

## Automatic access-token renewal

`AuthSession.EnsureSessionAsync` coordinates request-driven checks, a cancellable one-shot timer scheduled 30 seconds before access expiry, and browser visibility/focus events. The timer belongs to the client-scoped session, not a page. Browser suspension can delay it; resuming the tab or sending an authenticated request checks expiry again. Publishing a verified session replaces the schedule; clearing the session, logout and disposal cancel it.

Authenticated operations use `SendAuthenticatedAsync`. The existing manual `/me` check now uses this path. Only safe reads explicitly opt into one retry after a 401; 403 responses and mutations are not automatically retried. Login, refresh and the `/me` verification inside issuance use the raw `AuthApiClient` to avoid recursive refresh. Future authenticated API operations must use the session gateway rather than bypassing it.

Concurrent callers share the same ensure/refresh tasks. Cross-tab Web Locks still serialize rotation, reading the latest credential after acquiring the lock. Each tab retains its own in-memory access token, so different tabs can rotate sequentially; this design does not share access tokens through persistent storage. Logout invalidates outstanding results before waiting for the rotation lock and revoking the latest stored credential.

The existing conservative rotation failure policy remains: a failed or lost refresh response clears the stored credential and stops automatic renewal. A lost response can mean the server consumed the old token, so it is not retried automatically. This can require signing in again after a network failure, not only after token expiry. Refresh lifetimes remain governed by the backend; a sliding seven-day lifetime can keep extending while automatic renewal runs.

Manual checks: leave a signed-in page idle through the renewal time; resume a suspended tab after access expiry; trigger Check session near the timer deadline; open two signed-in tabs; sign out while rotation is in flight; reject/expire a refresh token; interrupt a refresh response. Check that only safe reads retry, expiry updates after renewal, and logout never restores authenticated UI. Do not copy tokens from browser tools into logs or reports.
