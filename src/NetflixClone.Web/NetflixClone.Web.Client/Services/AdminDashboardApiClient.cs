using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed class AdminDashboardApiClient(HttpClient http, AuthSession session)
{
    public async Task<ApiResult<AdminDashboardReply>> GetAsync(DateOnly from, DateOnly through, CancellationToken ct)
    {
        try
        {
            return await session.SendAuthenticatedAsync<AdminDashboardReply>(async token =>
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, $"api/admin/dashboard?from={from:yyyy-MM-dd}&through={through:yyyy-MM-dd}");
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    using var response = await http.SendAsync(request, ct);
                    if (!response.IsSuccessStatusCode)
                        return new(default, response.StatusCode == System.Net.HttpStatusCode.Forbidden ? "Current administrator access is required." : "Could not load dashboard. Check the date range and try again.", (int)response.StatusCode);
                    var value = await response.Content.ReadFromJsonAsync<AdminDashboardReply>(cancellationToken: ct);
                    return new(value, value is null ? "No dashboard data was returned." : null, (int)response.StatusCode);
                }
                catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
                { return new(default, "Could not load dashboard. Check your connection and try again.", 0); }
            }, retryUnauthorized: true);
        }
        catch (JSException) { return new(default, "Your browser session could not be checked. Reload and try again.", 0); }
    }
}
