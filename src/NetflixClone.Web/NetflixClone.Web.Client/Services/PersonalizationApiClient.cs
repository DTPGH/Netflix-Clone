using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed record OnboardingReply(bool OnboardingCompleted, int[] MovieIds);
public sealed record RecommendationsReply(MovieCard[] Items, string Source);
public sealed class PersonalizationApiClient(HttpClient http, AuthSession session)
{
    public Task<ApiResult<OnboardingReply>> StateAsync(int profileId, CancellationToken ct)
        => Get<OnboardingReply>($"api/profiles/{profileId}/onboarding", ct);
    public Task<ApiResult<MoviesReply>> MoviesAsync(int profileId, int page, string search, CancellationToken ct)
        => Get<MoviesReply>($"api/profiles/{profileId}/onboarding/movies?page={page}&pageSize=20&search={Uri.EscapeDataString(search)}", ct);
    public Task<ApiResult<RecommendationsReply>> RecommendationsAsync(int profileId, CancellationToken ct)
        => Get<RecommendationsReply>($"api/profiles/{profileId}/recommendations?limit=12", ct);
    public Task<ApiResult<OnboardingReply>> CompleteAsync(int profileId, int[] movieIds, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => Send<OnboardingReply>(HttpMethod.Put, $"api/profiles/{profileId}/onboarding",
            new { movieIds }, token, ct));
    private Task<ApiResult<T>> Get<T>(string path, CancellationToken ct)
        => session.SendAuthenticatedAsync(token => Send<T>(HttpMethod.Get, path, null, token, ct), retryUnauthorized: true);
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
                string? code = null;
                try
                {
                    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    if (json.RootElement.TryGetProperty("code", out var property) && property.ValueKind == JsonValueKind.String)
                        code = property.GetString();
                }
                catch (JsonException) { }
                var status = (int)response.StatusCode;
                return new(default, code switch
                {
                    "Personalization.ProfileNotFound" => "This profile is no longer available. Choose a profile again.",
                    "Onboarding.InvalidMovies" => "Some movies are no longer eligible. Reload and choose again.",
                    "Onboarding.InvalidSelection" => "Choose up to five different movies, or skip.",
                    _ when status == 409 => "Onboarding or this profile changed. Reload to get the latest state.",
                    _ when status == 401 => "Please sign in again.",
                    _ when status == 400 => "Check your selection or search.",
                    _ => "The request could not be confirmed. Please reload."
                }, status, code);
            }
            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return data is null ? new(default, "Could not read the response. Please reload.", 0)
                : new(data, null, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(default, "Could not confirm the request. Check your connection and reload.", 0);
        }
    }
}
