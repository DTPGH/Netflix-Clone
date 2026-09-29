using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed record WatchProgressReply(int MovieId, int PositionSeconds, bool IsCompleted, DateTime? UpdatedAtUtc);
public sealed record ContinueWatchingCard(MovieCard Movie, int PositionSeconds, DateTime LastWatchedAtUtc);
public sealed record ContinueWatchingReply(ContinueWatchingCard[] Items);
public sealed class WatchHistoryApiClient(HttpClient http, AuthSession session)
{
    public Task<ApiResult<WatchProgressReply>> GetAsync(int profileId, int movieId, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => Send<WatchProgressReply>(HttpMethod.Get,
            $"api/profiles/{profileId}/watch-history/{movieId}", null, token, ct), retryUnauthorized: true);
    public Task<ApiResult<ContinueWatchingReply>> ContinueAsync(int profileId, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => Send<ContinueWatchingReply>(HttpMethod.Get,
            $"api/profiles/{profileId}/continue-watching", null, token, ct), retryUnauthorized: true);
    public Task<ApiResult<WatchProgressReply>> SaveAsync(int profileId, int movieId, int positionSeconds,
        int durationSeconds, bool ended, DateTime? expectedUpdatedAtUtc, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => Send<WatchProgressReply>(HttpMethod.Put,
            $"api/profiles/{profileId}/watch-history/{movieId}",
            new { positionSeconds, durationSeconds, ended, expectedUpdatedAtUtc }, token, ct));
    private async Task<ApiResult<T>> Send<T>(HttpMethod method, string path, object? body, string token, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                return new(default, status switch {
                    409 => "Progress changed in another request. Reload playback before saving again.",
                    404 => "This movie or profile is no longer available for playback.",
                    401 => "Please sign in again.",
                    _ => "Progress could not be confirmed. Reload playback to recover the saved position."
                }, status);
            }
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return value is null ? new(default, "Could not read saved progress. Please reload.", 0)
                : new(value, null, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(default, "Could not confirm progress. Check your connection and reload playback.", 0);
        }
    }
}
