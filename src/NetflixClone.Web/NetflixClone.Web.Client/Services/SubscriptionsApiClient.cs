using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed class SubscriptionsApiClient(HttpClient http, AuthSession session)
{
    public Task<ApiResult<SubscriptionPlan[]>> PlansAsync(CancellationToken ct) => Send<SubscriptionPlan[]>("plans", null, ct);
    public Task<ApiResult<SubscriptionOverview>> CurrentAsync(CancellationToken ct) => Send<SubscriptionOverview>("current", null, ct);
    public Task<ApiResult<SubscriptionPaymentPage>> PaymentsAsync(int page, CancellationToken ct) => Send<SubscriptionPaymentPage>($"payments?page={page}", null, ct);
    public Task<ApiResult<SubscriptionPurchaseResult>> PurchaseAsync(SubscriptionPurchaseDraft draft, CancellationToken ct) =>
        Send<SubscriptionPurchaseResult>("purchase", new { draft.PlanId, draft.IdempotencyKey, draft.ExpectedPlanUpdatedAtUtc }, ct);
    private async Task<ApiResult<T>> Send<T>(string path, object? body, CancellationToken ct)
    {
        try
        {
            return await session.SendAuthenticatedAsync<T>(async token =>
            {
                try
                {
                    using var request = new HttpRequestMessage(body is null ? HttpMethod.Get : HttpMethod.Post, "api/subscriptions/" + path);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    if (body is not null) request.Content = JsonContent.Create(body);
                    using var response = await http.SendAsync(request, ct);
                    var status = (int)response.StatusCode;
                    if (!response.IsSuccessStatusCode)
                    {
                        string? description = null, code = null;
                        try { using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                            if (json.RootElement.TryGetProperty("description", out var d)) description = d.GetString();
                            if (json.RootElement.TryGetProperty("code", out var c)) code = c.GetString(); }
                        catch (JsonException) { }
                        return new(default, description ?? (status == 403 ? "Your account is not eligible for this action." :
                            "The result could not be confirmed. Retry the same purchase to check it."), status, code);
                    }
                    var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                    return new(value, value is null ? "The result could not be confirmed." : null, status);
                }
                catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
                { return new(default, "The result could not be confirmed. Check your connection and retry the same purchase.", 0); }
            }, retryUnauthorized: true); // POST retry is safe because the request key and payload are unchanged.
        }
        catch (JSException) { return new(default, "Your browser session could not be checked. Reload and try again.", 0); }
    }
}
