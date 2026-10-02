using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed class AdminUsersApiClient(HttpClient http, AuthSession session)
{
    public Task<ApiResult<AdminUserPage>> ListAsync(int page, string search, bool? locked, CancellationToken ct)
        => Send<AdminUserPage>(HttpMethod.Get, $"api/admin/users?page={page}&search={Uri.EscapeDataString(search)}" + (locked.HasValue ? $"&isLocked={locked.Value.ToString().ToLowerInvariant()}" : ""), null, true, ct);
    public Task<ApiResult<AdminUser>> GetAsync(int id, CancellationToken ct) => Send<AdminUser>(HttpMethod.Get, $"api/admin/users/{id}", null, true, ct);
    public Task<ApiResult<AdminUserRole[]>> RolesAsync(CancellationToken ct) => Send<AdminUserRole[]>(HttpMethod.Get, "api/admin/users/roles", null, true, ct);
    public Task<ApiResult<AdminUser>> LockAsync(AdminUser user, bool locked, string reason, CancellationToken ct)
        => Send<AdminUser>(HttpMethod.Put, $"api/admin/users/{user.UserAccountId}/lock", new { IsLocked = locked, Reason = reason, ExpectedUpdatedAtUtc = user.UpdatedAtUtc }, false, ct);
    public Task<ApiResult<AdminUser>> ChangeRolesAsync(AdminUser user, int[] roleIds, string reason, CancellationToken ct)
        => Send<AdminUser>(HttpMethod.Put, $"api/admin/users/{user.UserAccountId}/roles", new { RoleIds = roleIds, Reason = reason, ExpectedUpdatedAtUtc = user.UpdatedAtUtc }, false, ct);
    public Task<ApiResult<AdminLogPage>> LogsAsync(int page, int? actor, int? target, string action, CancellationToken ct)
        => Send<AdminLogPage>(HttpMethod.Get, $"api/admin/action-logs?page={page}" + (actor.HasValue ? $"&actorId={actor}" : "") +
            (target.HasValue ? $"&targetId={target}" : "") + (action.Length > 0 ? $"&action={Uri.EscapeDataString(action)}" : ""), null, true, ct);
    private async Task<ApiResult<T>> Send<T>(HttpMethod method, string path, object? body, bool retry, CancellationToken ct)
    {
        try { return await session.SendAuthenticatedAsync<T>(async token =>
        {
            try
            {
                using var request = new HttpRequestMessage(method, path);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                if (body is not null) request.Content = JsonContent.Create(body);
                using var response = await http.SendAsync(request, ct);
                var status = (int)response.StatusCode;
                if (!response.IsSuccessStatusCode)
                {
                    string? description = null;
                    try
                    {
                        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                        if (json.RootElement.TryGetProperty("description", out var d)) description = d.GetString();
                    } catch (JsonException) { }
                    return new(default, description ?? (status switch { 401 => "Please sign in again.", 403 => "Current administrator access is required.",
                        404 => "This account is unavailable.", 409 => "The account changed. Reload before retrying.",
                        _ => "The result could not be confirmed. Reload before retrying." }), status);
                }
                var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                return value is null ? new(default, "The service returned an empty response.", 0) : new(value, null, status);
            } catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
            { return new(default, "The result could not be confirmed. Check your connection and reload before retrying.", 0); }
        }, retry); } catch (JSException) { return new(default, "Your browser session could not be checked. Reload and try again.", 0); }
    }
}
