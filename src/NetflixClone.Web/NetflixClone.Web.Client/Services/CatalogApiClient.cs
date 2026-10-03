using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed class CatalogApiClient(HttpClient http, AuthSession session)
{
    public Task<ApiResult<MoviesReply>> BrowseProfileAsync(int profileId, int page, string search, int? genre, string sort, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => SendAsync<MoviesReply>(HttpMethod.Get,
            $"api/profiles/{profileId}/movies?page={page}&pageSize=20&search={Uri.EscapeDataString(search)}&sort={Uri.EscapeDataString(sort)}" +
            (genre.HasValue ? $"&genreId={genre.Value}" : ""), token, ct), retryUnauthorized: true);
    public Task<ApiResult<MoviesReply>> BrowseAsync(int page, string search, int? genre, string sort, CancellationToken ct)
        => SendAsync<MoviesReply>(HttpMethod.Get, $"api/movies?page={page}&pageSize=20&search={Uri.EscapeDataString(search)}&sort={Uri.EscapeDataString(sort)}" +
            (genre.HasValue ? $"&genreId={genre.Value}" : ""), null, ct);
    public Task<ApiResult<GenresReply>> GenresAsync(CancellationToken ct)
        => SendAsync<GenresReply>(HttpMethod.Get, "api/genres", null, ct);
    public Task<ApiResult<MovieReply>> DetailAsync(int profileId, int id, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => SendAsync<MovieReply>(HttpMethod.Get, $"api/profiles/{profileId}/movies/{id}", token, ct), retryUnauthorized: true);
    public Task<ApiResult<PlaybackReply>> PlaybackAsync(int profileId, int id, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => SendAsync<PlaybackReply>(HttpMethod.Post, $"api/profiles/{profileId}/movies/{id}/playback", token, ct));
    public string? PlaybackUrl(string value, int movieId)
    {
        if (http.BaseAddress is null || !Uri.TryCreate(http.BaseAddress, value, out var uri) ||
            uri.Scheme != http.BaseAddress.Scheme || uri.Authority != http.BaseAddress.Authority ||
            uri.AbsolutePath != $"/api/media/{movieId}" || !uri.Query.StartsWith("?ticket=", StringComparison.Ordinal) ||
            uri.Fragment.Length != 0) return null;
        return uri.AbsoluteUri;
    }
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, string? token, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                return new(default, status switch {
                    401 => "Please sign in to watch this demo.",
                    403 => "An active subscription is required to play this video. Check your subscription and try again.",
                    404 => "This movie is unavailable for this profile.",
                    409 => "No playable demo video is available for this movie.",
                    400 => "Check your search and filter values.",
                    _ => "The movie service is unavailable. Please try again."
                }, status);
            }
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return value is null ? new(default, "The service returned an empty response.", 0) : new(value, null, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(default, "Could not load movies. Check your connection and try again.", 0);
        }
    }
}
