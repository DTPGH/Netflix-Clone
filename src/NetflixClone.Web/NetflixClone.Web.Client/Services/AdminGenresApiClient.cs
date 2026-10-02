using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed class AdminGenresApiClient(HttpClient http, AuthSession session)
{
    public Task<ApiResult<AdminGenre[]>> ListAsync(CancellationToken ct) => Send<AdminGenre[]>(HttpMethod.Get, "api/admin/genres", null, true, ct);
    public Task<ApiResult<AdminGenre>> SaveAsync(AdminGenre? existing, string name, CancellationToken ct) => Send<AdminGenre>(
        existing is null ? HttpMethod.Post : HttpMethod.Put, "api/admin/genres" + (existing is null ? "" : $"/{existing.GenreId}"),
        new { Name = name, ExpectedName = existing?.Name }, false, ct);
    public Task<ApiResult<object>> DeleteAsync(AdminGenre genre, CancellationToken ct) => Send<object>(HttpMethod.Delete,
        $"api/admin/genres/{genre.GenreId}?expectedName={Uri.EscapeDataString(genre.Name)}", null, false, ct);
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
                    string? code = null, description = null;
                    try
                    {
                        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                        if (json.RootElement.TryGetProperty("description", out var d)) description = d.GetString();
                        if (json.RootElement.TryGetProperty("code", out var c)) code = c.GetString();
                    } catch (JsonException) { }
                    return new(default, description ?? (status switch { 401 => "Please sign in again.", 403 => "Administrator access is required.",
                        404 => "This genre no longer exists.", _ => "The result could not be confirmed. Reload before retrying." }), status, code);
                }
                if (status == 204) return new(default, null, status);
                var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                return value is null ? new(default, "The service returned an empty response.", 0) : new(value, null, status);
            } catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
            { return new(default, "The result could not be confirmed. Check your connection and reload before retrying.", 0); }
        }, retry); } catch (JSException) { return new(default, "Your browser session could not be checked. Reload and try again.", 0); }
    }
}
