using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using NetflixClone.Web.Client.Models;

namespace NetflixClone.Web.Client.Services;

public sealed class DevicesApiClient(HttpClient http, AuthSession session)
{
    public Task<ApiResult<DevicesReply>> ListAsync(CancellationToken ct = default)
        => SendAuthenticatedAsync<DevicesReply>(HttpMethod.Get, "api/auth/devices", true, ct);

    public Task<ApiResult<object>> RevokeAsync(int deviceId, CancellationToken ct = default)
        => SendAuthenticatedAsync<object>(HttpMethod.Post, $"api/auth/devices/{deviceId}/revoke", false, ct);

    private async Task<ApiResult<T>> SendAuthenticatedAsync<T>(HttpMethod method, string path, bool retry, CancellationToken ct)
    {
        try
        {
            return await session.SendAuthenticatedAsync(token => SendAsync<T>(method, path, token, ct),
                retryUnauthorized: retry);
        }
        catch (JSException)
        {
            return new(default, "Your browser session could not be checked. Reload the page before trying again.", 0);
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, string token, CancellationToken ct)
    {
        try
        {
            // A fresh request is needed if a safe GET is retried after refresh.
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string? code = null;
                try
                {
                    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    if (json.RootElement.TryGetProperty("code", out var value) && value.ValueKind == JsonValueKind.String)
                        code = value.GetString();
                }
                catch (JsonException) { }
                var message = (int)response.StatusCode switch
                {
                    401 => "Please sign in again to manage your devices.",
                    403 => "You do not have permission to manage these devices.",
                    404 => "This device is no longer available. Review the updated list.",
                    409 => "This device changed while we were signing it out. Review the updated list and try again.",
                    _ => "The service could not confirm the result. Reload the list before trying again."
                };
                return new(default, message, (int)response.StatusCode, code);
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return new(default, null, 204);
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return data is null ? new(default, "The result could not be confirmed. Reload the list.", 0)
                : new(data, null, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(default, "The result could not be confirmed. Check your connection and reload the list before trying again.", 0);
        }
    }
}

