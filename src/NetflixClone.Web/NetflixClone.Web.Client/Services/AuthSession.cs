using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using NetflixClone.Web.Client.Models;

namespace NetflixClone.Web.Client.Services;

// Client-only scoped service. Neither this instance nor its tokens are serialized for prerendering.
public sealed class AuthSession : AuthenticationStateProvider, IDisposable
{
    private readonly AuthApiClient _api;
    private readonly ICredentialStore _store;
    private ClaimsPrincipal _user = new(new ClaimsIdentity());
    private string? _accessToken;
    private string _epoch = "";
    private long _version;
    private Task? _initialization;
    private Task<bool>? _refreshTask;
    private Task<bool>? _verification;
    private string? _logoutNotice;
    private Task<bool>? _ensuring;
    private CancellationTokenSource? _refreshSchedule;
    private bool _disposed;
    private static readonly TimeSpan RefreshAhead = TimeSpan.FromSeconds(30);
    public event Action? Changed;
    public bool Initialized { get; private set; }
    public bool StorageReady { get; private set; }
    public bool IsLoggingOut { get; private set; }
    public bool IsAuthenticated => _user.Identity?.IsAuthenticated == true;
    public DateTime? AccessExpiresAtUtc { get; private set; }
    public string? Notice { get; private set; }

    public AuthSession(AuthApiClient api, ICredentialStore store)
    {
        _api = api;
        _store = store;
        _store.SessionChanged += SessionChanged;
        _store.Resumed += OnResumedAsync;
    }
    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(_user));
    public Task InitializeAsync() => _initialization ??= InitializeCoreAsync();

    private async Task InitializeCoreAsync()
    {
        try
        {
            await _store.InitializeAsync();
            StorageReady = true;
            await RefreshSharedAsync();
        }
        catch (JSException) { StorageReady = false; Notice = "Browser storage is unavailable. Enable site storage and use a current browser over HTTPS."; }
        finally { Initialized = true; Changed?.Invoke(); }
    }

    public async Task<string?> LoginAsync(LoginForm form)
    {
        await InitializeAsync();
        if (!StorageReady) return Notice;
        if (IsLoggingOut) return "Please wait for sign out to finish.";
        var version = _version;
        try
        {
            await using var lease = await _store.AcquireAsync();
            if (version != _version || IsLoggingOut) return "Sign in was cancelled. Please try again.";
            await _store.ChangeEpochAsync();
            var epoch = await _store.GetEpochAsync();
            var device = await _store.GetDeviceAsync();
            var reply = await _api.LoginAsync(new(form.Email, form.Password, device, form.DeviceName, "Web"));
            if (!reply.Success) return reply.Error;
            var value = reply.Value!;
            await _store.SetDeviceAsync(value.DeviceIdentifier);
            // Persist the newest token before /me. Logout waits for this lease and revokes this token,
            // including when its response arrives after the user has clicked Sign out.
            await _store.WriteAsync(new(value.RefreshToken, value.RefreshTokenExpiresAtUtc));
            if (!await CanPublishAsync(version, epoch)) return "Sign in was cancelled.";
            var me = await _api.MeAsync(value.AccessToken);
            if (!me.Success)
            {
                SetAnonymous();
                if (me.Status == 401) await _store.ClearAsync();
                return me.Error;
            }
            if (!await CanPublishAsync(version, epoch)) return "Sign in was cancelled.";
            Publish(me.Value!, value.AccessToken, value.ExpiresAtUtc, epoch);
            return null;
        }
        catch (JSException)
        {
            SetAnonymous();
            return "Could not save this session in your browser. Enable site storage and sign in again.";
        }
    }

    public Task<bool> VerifyAsync()
    {
        if (_verification is { IsCompleted: false }) return _verification;
        return _verification = VerifyCoreAsync();
    }
    private async Task<bool> VerifyCoreAsync()
    {
        var me = await SendAuthenticatedAsync(_api.MeAsync, retryUnauthorized: true);
        if (!me.Success)
        {
            Notice = me.Error;
            Changed?.Invoke();
        }
        return me.Success;
    }

    // All authenticated API operations enter here. Retry is opt-in for safe reads only.
    public async Task<ApiResult<T>> SendAuthenticatedAsync<T>(
        Func<string, Task<ApiResult<T>>> send, bool retryUnauthorized = false)
    {
        if (!await EnsureSessionAsync()) return new(default, Notice ?? "Please sign in again.", 401);
        var version = _version;
        var token = _accessToken!;
        var result = await send(token);
        if (!await CanPublishAsync(version, _epoch)) return new(default, "The session changed. Please try again.", 401);
        if (result.Status == 401 && retryUnauthorized)
        {
            // Another caller may already have replaced the rejected access token.
            if (_accessToken == token && !await RefreshSharedAsync())
                return new(default, Notice ?? "Please sign in again.", 401);
            if (!await CanPublishAsync(version, _epoch) || _accessToken is null)
                return new(default, "Please sign in again.", 401);
            result = await send(_accessToken);
            if (!await CanPublishAsync(version, _epoch)) return new(default, "The session changed. Please try again.", 401);
        }
        if (result.Status == 401) SetAnonymous();
        return result;
    }

    public Task<bool> EnsureSessionAsync()
    {
        if (_ensuring is { IsCompleted: false }) return _ensuring;
        return _ensuring = EnsureSessionCoreAsync();
    }

    private async Task<bool> EnsureSessionCoreAsync()
    {
        await InitializeAsync();
        if (!StorageReady || IsLoggingOut || _disposed) return false;
        try
        {
            if (_accessToken is null || AccessExpiresAtUtc <= DateTime.UtcNow.Add(RefreshAhead) ||
                _epoch != await _store.GetEpochAsync()) return await RefreshSharedAsync();
            return true;
        }
        catch (JSException) { SetAnonymous(); Notice = "Session storage is unavailable. Please sign in again."; Changed?.Invoke(); return false; }
    }
    private Task<bool> RefreshSharedAsync()
    {
        if (IsLoggingOut || _disposed) return Task.FromResult(false);
        if (_refreshTask is { IsCompleted: false }) return _refreshTask;
        return _refreshTask = RefreshCoreAsync();
    }
    private async Task<bool> RefreshCoreAsync()
    {
        var version = _version;
        await using var lease = await _store.AcquireAsync();
        if (version != _version || IsLoggingOut || _disposed) return false;
        var epoch = await _store.GetEpochAsync();
        // Read AFTER taking the cross-tab lock: another tab may have already rotated.
        var stored = await _store.ReadAsync();
        if (stored is null) { SetAnonymous(); return false; }
        if (stored.RefreshTokenExpiresAtUtc <= DateTime.UtcNow)
        {
            await _store.ClearAsync();
            Notice = "Your session has ended. Please sign in again.";
            SetAnonymous();
            return false;
        }
        var reply = await _api.RefreshAsync(stored.RefreshToken);
        if (!reply.Success)
        {
            // A lost response may mean the old credential was consumed. Never retry it automatically.
            await _store.ClearAsync();
            Notice = reply.Error;
            SetAnonymous();
            return false;
        }
        var value = reply.Value!;
        await _store.WriteAsync(new(value.RefreshToken, value.RefreshTokenExpiresAtUtc));
        if (!await CanPublishAsync(version, epoch)) return false;
        var me = await _api.MeAsync(value.AccessToken);
        if (!me.Success)
        {
            if (me.Status == 401) await _store.ClearAsync();
            Notice = me.Error;
            SetAnonymous();
            return false;
        }
        if (!await CanPublishAsync(version, epoch)) return false;
        Publish(me.Value!, value.AccessToken, value.ExpiresAtUtc, epoch);
        return true;
    }

    public async Task LogoutAsync()
    {
        if (IsLoggingOut) return;
        IsLoggingOut = true;
        ++_version; // invalidate all in-flight responses before the first await
        SetAnonymous();
        try
        {
            await InitializeAsync();
            if (!StorageReady) { Notice = "Signed out here. Browser storage could not be cleared; clear this site's data before leaving a shared device."; return; }
            await _store.ChangeEpochAsync(); // invalidate other tabs immediately
            await using var lease = await _store.AcquireAsync(); // drain pending rotation first
            try
            {
                var latest = await _store.ReadAsync();
                var reply = latest is null ? null : await _api.LogoutAsync(latest.RefreshToken);
                Notice = reply is { Success: false }
                    ? "Signed out of this browser. The server could not confirm revocation; the previous token may remain valid."
                    : "You have signed out.";
            }
            finally { await _store.ClearAsync(); }
        }
        catch (JSException) { Notice = "Signed out here. Clear this site's browser data to remove any saved credentials."; }
        finally { IsLoggingOut = false; _logoutNotice = Notice; SetAnonymous(); }
    }

    public void DismissLogoutNotice()
    {
        // A later session/storage error must not be dismissed with the logout message.
        if (_logoutNotice is not null && Notice == _logoutNotice)
        {
            Notice = null;
            Changed?.Invoke();
        }
        _logoutNotice = null;
    }

    private async Task<bool> CanPublishAsync(long version, string epoch)
    {
        var currentEpoch = await _store.GetEpochAsync();
        return version == _version && !IsLoggingOut && !_disposed && epoch == currentEpoch;
    }
    private void Publish(MeReply me, string token, DateTime expires, string epoch)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, me.UserAccountId), new(ClaimTypes.Name, "Account " + me.UserAccountId) };
        claims.AddRange(me.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        _user = new ClaimsPrincipal(new ClaimsIdentity(claims, "VerifiedApi"));
        _accessToken = token;
        AccessExpiresAtUtc = expires;
        _epoch = epoch;
        Notice = null;
        ScheduleRefresh();
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        Changed?.Invoke();
    }
    private void SetAnonymous()
    {
        StopRefreshSchedule();
        _accessToken = null;
        AccessExpiresAtUtc = null;
        _user = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        Changed?.Invoke();
    }
    private void SessionChanged()
    {
        ++_version;
        Notice = "Your session changed in another tab. Sign in or check your saved session.";
        SetAnonymous();
    }
    private async Task OnResumedAsync()
    {
        // Do not silently sign back in after logout or after a failed rotation.
        if (IsAuthenticated && !_disposed && !IsLoggingOut) await EnsureSessionAsync();
    }

    private void ScheduleRefresh()
    {
        StopRefreshSchedule();
        if (_disposed || IsLoggingOut || AccessExpiresAtUtc is null) return;
        _refreshSchedule = new CancellationTokenSource();
        var delay = AccessExpiresAtUtc.Value - DateTime.UtcNow - RefreshAhead;
        // A minimum delay prevents a tight loop if the server returns an unexpected expiry.
        _ = RefreshWhenDueAsync(delay > TimeSpan.FromSeconds(1) ? delay : TimeSpan.FromSeconds(1),
            _refreshSchedule.Token);
    }

    private async Task RefreshWhenDueAsync(TimeSpan delay, CancellationToken cancellation)
    {
        try
        {
            await Task.Delay(delay, cancellation);
            if (cancellation.IsCancellationRequested) return;
            await EnsureSessionAsync();
            // Recheck if the clock moved backwards and this token is not due yet.
            if (!cancellation.IsCancellationRequested && IsAuthenticated) ScheduleRefresh();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
    }

    private void StopRefreshSchedule()
    {
        _refreshSchedule?.Cancel();
        _refreshSchedule?.Dispose();
        _refreshSchedule = null;
    }

    public void Dispose()
    {
        _disposed = true;
        ++_version;
        StopRefreshSchedule();
        _store.SessionChanged -= SessionChanged;
        _store.Resumed -= OnResumedAsync;
    }
}
