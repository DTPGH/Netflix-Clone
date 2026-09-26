using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed record MyListStatusReply(bool IsInMyList);
public sealed class MyListApiClient(HttpClient http, AuthSession session)
{
    public Task<ApiResult<MoviesReply>> ListAsync(int profileId, int page, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => SendAsync<MoviesReply>(HttpMethod.Get,
            $"api/profiles/{profileId}/my-list?page={page}&pageSize=20", token, ct), retryUnauthorized: true);
    public Task<ApiResult<MyListStatusReply>> StatusAsync(int profileId, int movieId, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => SendAsync<MyListStatusReply>(HttpMethod.Get,
            $"api/profiles/{profileId}/my-list/{movieId}", token, ct), retryUnauthorized: true);
    public Task<ApiResult<object>> SetAsync(int profileId, int movieId, bool included, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => SendAsync<object>(included ? HttpMethod.Put : HttpMethod.Delete,
            $"api/profiles/{profileId}/my-list/{movieId}", token, ct));
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, string token, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string? code = null;
                try
                {
                    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    if (json.RootElement.TryGetProperty("code", out var value) && value.ValueKind == JsonValueKind.String) code = value.GetString();
                }
                catch (JsonException) { }
                var status = (int)response.StatusCode;
                return new(default, code switch {
                    "MyList.ProfileNotFound" => "This profile is no longer available. Choose a profile again.",
                    "MyList.MovieNotFound" => "This movie is no longer available.",
                    "MyList.InvalidPage" => "This page is invalid.",
                    _ when status == 401 => "Please sign in again.",
                    _ => "The request could not be confirmed. Reload before trying again."
                }, status, code);
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return new(default, null, 204);
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return data is null ? new(default, "Could not read My List. Please reload.", 0) : new(data, null, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(default, "Could not confirm the request. Check your connection and reload.", 0);
        }
    }
}
