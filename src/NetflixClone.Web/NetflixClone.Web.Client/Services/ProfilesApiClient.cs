using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;

public sealed class ProfilesApiClient(HttpClient http, AuthSession session)
{
    public Task<ApiResult<ProfilesReply>> ListAsync(CancellationToken ct = default)
        => session.SendAuthenticatedAsync(token => SendAsync<ProfilesReply>(HttpMethod.Get, "", token, null, ct), retryUnauthorized: true);
    public Task<ApiResult<ProfileReply>> CreateAsync(ProfilePayload payload, CancellationToken ct = default)
        => session.SendAuthenticatedAsync(token => SendAsync<ProfileReply>(HttpMethod.Post, "", token, payload, ct));
    public Task<ApiResult<ProfileReply>> UpdateAsync(int id, ProfilePayload payload, CancellationToken ct = default)
        => session.SendAuthenticatedAsync(token => SendAsync<ProfileReply>(HttpMethod.Put, "/" + id, token, payload, ct));
    public Task<ApiResult<object>> DeleteAsync(int id, CancellationToken ct = default)
        => session.SendAuthenticatedAsync(token => SendAsync<object>(HttpMethod.Delete, "/" + id, token, null, ct));

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, string token, object? body, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, "api/profiles" + path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null) request.Content = JsonContent.Create(body);
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
                var message = code switch
                {
                    "Profiles.InvalidName" => "Enter a name between 1 and 100 characters.",
                    "Profiles.LimitReached" => "You already have 5 profiles. Delete one before adding another.",
                    "Profiles.ConcurrentChange" => "This profile changed elsewhere. Reload the list and review the latest details before saving again.",
                    "Profiles.NotFound" => "This profile is no longer available. Reload the list.",
                    _ when (int)response.StatusCode == 401 => "Please sign in again to manage your profiles.",
                    _ when (int)response.StatusCode == 403 => "You do not have permission to manage these profiles.",
                    _ when (int)response.StatusCode >= 500 => "The service could not confirm the result. Reload the list before trying again.",
                    _ => "The request could not be completed. Check the form and try again."
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
