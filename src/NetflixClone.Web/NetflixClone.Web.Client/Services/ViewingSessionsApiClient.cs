using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed record ViewingSessionReply(int SessionId, int WatchedSeconds, bool IsQualifiedView, bool IsEnded, long Sequence);
public sealed class ViewingSessionsApiClient(HttpClient http, AuthSession auth, ICredentialStore store)
{
    public async Task<ViewingSessionReply?> StartAsync(int profileId, int movieId, Guid clientId, CancellationToken ct)
    {
        var identifier = await store.GetDeviceAsync();
        if (identifier is null) return null;
        var result = await auth.SendAuthenticatedAsync(token => Send(HttpMethod.Post,
            $"api/profiles/{profileId}/movies/{movieId}/viewing-sessions", new { deviceIdentifier = identifier, clientSessionId = clientId }, token, ct), retryUnauthorized: true);
        return result.Success ? result.Value : null;
    }
    public async Task<ViewingSessionReply?> CheckpointAsync(int profileId, int movieId, int id, long sequence, long watchedMilliseconds, string? endReason, CancellationToken ct)
    {
        var identifier = await store.GetDeviceAsync();
        if (identifier is null) return null;
        var result = await auth.SendAuthenticatedAsync(token => Send(HttpMethod.Put,
            $"api/profiles/{profileId}/movies/{movieId}/viewing-sessions/{id}/progress",
            new { deviceIdentifier = identifier, sequence, watchedMilliseconds, endReason }, token, ct), retryUnauthorized: true);
        return result.Success ? result.Value : null;
    }
    private async Task<ApiResult<ViewingSessionReply>> Send(HttpMethod method, string path, object body, string token, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return new(default, "Viewing activity could not be saved.", (int)response.StatusCode);
            var value = await response.Content.ReadFromJsonAsync<ViewingSessionReply>(cancellationToken: ct);
            return new(value, value is null ? "Viewing activity could not be saved." : null, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        { return new(default, "Viewing activity could not be saved.", 0); }
    }
}
