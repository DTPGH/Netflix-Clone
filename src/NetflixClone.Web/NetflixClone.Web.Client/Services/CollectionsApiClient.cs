using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed class CollectionsApiClient(HttpClient http, AuthSession session)
{
    public Task<ApiResult<CollectionPage>> ListAsync(int page, CancellationToken ct) => Send<CollectionPage>(HttpMethod.Get, $"api/admin/movie-collections?page={page}", null, true, ct);
    public Task<ApiResult<CollectionDetail>> GetAsync(int id, CancellationToken ct) => Send<CollectionDetail>(HttpMethod.Get, $"api/admin/movie-collections/{id}", null, true, ct);
    public Task<ApiResult<CollectionDetail>> SaveAsync(int? id, SaveCollectionPayload body, CancellationToken ct)
        => Send<CollectionDetail>(id.HasValue ? HttpMethod.Put : HttpMethod.Post, "api/admin/movie-collections" + (id.HasValue ? $"/{id}" : ""), body, false, ct);
    public Task<ApiResult<BrowseCollection[]>> BrowseAsync(int profileId, CancellationToken ct)
        => Send<BrowseCollection[]>(HttpMethod.Get, $"api/profiles/{profileId}/movie-collections", null, true, ct);
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
                    }
                    catch (JsonException) { }
                    return new(default, description ?? (status switch { 401 => "Please sign in again.", 403 => "Administrator access is required.",
                        404 => "This collection or profile is unavailable.", 409 => "The collection changed. Reload before saving.",
                        _ => "The result could not be confirmed. Reload before retrying." }), status);
                }
                var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                return value is null ? new(default, "The service returned an empty response.", 0) : new(value, null, status);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
            { return new(default, "The result could not be confirmed. Check your connection and reload before retrying.", 0); }
        }, retry); }
        catch (JSException) { return new(default, "Your browser session could not be checked. Reload and try again.", 0); }
    }
}
