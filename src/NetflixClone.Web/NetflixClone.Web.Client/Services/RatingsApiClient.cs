using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed record RatingReply(int MovieId, string? Value);
public sealed class RatingsApiClient(HttpClient http, AuthSession session)
{
    public Task<ApiResult<RatingReply>> GetAsync(int profileId, int movieId, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => SendAsync(HttpMethod.Get, profileId, movieId, null, token, ct), retryUnauthorized: true);
    public Task<ApiResult<RatingReply>> SetAsync(int profileId, int movieId, string? value, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => SendAsync(value is null ? HttpMethod.Delete : HttpMethod.Put,
            profileId, movieId, value, token, ct));
    private async Task<ApiResult<RatingReply>> SendAsync(HttpMethod method, int profileId, int movieId,
        string? value, string token, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, $"api/profiles/{profileId}/ratings/{movieId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (method == HttpMethod.Put) request.Content = JsonContent.Create(new { value });
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string? code = null;
                try
                {
                    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    if (json.RootElement.TryGetProperty("code", out var property) && property.ValueKind == JsonValueKind.String)
                        code = property.GetString();
                }
                catch (JsonException) { }
                var status = (int)response.StatusCode;
                return new(null, code switch
                {
                    "Ratings.ProfileNotFound" => "This profile is no longer available. Choose a profile again.",
                    "Ratings.MovieNotFound" => "This movie is no longer available.",
                    "Ratings.InvalidValue" => "Choose Not for me, Like or Love.",
                    _ when status == 409 => "Your rating changed in another request. Reload before choosing again.",
                    _ when status == 401 => "Please sign in again.",
                    _ => "The rating could not be confirmed. Reload before trying again."
                }, status, code);
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return new(new(movieId, null), null, 204);
            var data = await response.Content.ReadFromJsonAsync<RatingReply>(cancellationToken: ct);
            if (data is null || data.MovieId != movieId || data.Value is not (null or "NotForMe" or "Like" or "Love"))
                return new(null, "Could not read the rating. Please reload.", 0);
            return new(data, null, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(null, "Could not confirm the rating. Check your connection and reload.", 0);
        }
    }
}
