using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using NetflixClone.Web.Client.Models;
namespace NetflixClone.Web.Client.Services;
public sealed class AdminViewingReportsApiClient(HttpClient http, AuthSession session)
{
    private static string Query(DateTime from, DateTime to) => $"fromUtc={Uri.EscapeDataString(from.ToString("O", CultureInfo.InvariantCulture))}&toUtc={Uri.EscapeDataString(to.ToString("O", CultureInfo.InvariantCulture))}";
    public Task<ApiResult<AdminViewingReport>> GetAsync(DateTime from, DateTime to, int page, CancellationToken ct) =>
        Send<AdminViewingReport>($"api/admin/reports/views?{Query(from, to)}&page={page}&pageSize=20", false, ct);
    public Task<ApiResult<byte[]>> ExportAsync(DateTime from, DateTime to, CancellationToken ct) =>
        Send<byte[]>($"api/admin/reports/views/export?{Query(from, to)}", true, ct);
    private async Task<ApiResult<T>> Send<T>(string path, bool file, CancellationToken ct)
    {
        try
        {
            return await session.SendAuthenticatedAsync<T>(async token =>
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, path);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    using var response = await http.SendAsync(request, ct);
                    var status = (int)response.StatusCode;
                    if (!response.IsSuccessStatusCode)
                    {
                        string? error = null;
                        try { using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                            if (json.RootElement.TryGetProperty("description", out var d)) error = d.GetString(); }
                        catch (JsonException) { }
                        return new(default, error ?? (status == 403 ? "Current administrator access is required." : "The report could not be loaded. Please try again."), status);
                    }
                    if (file)
                    {
                        if (response.Content.Headers.ContentType?.MediaType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                            return new(default, "The service did not return an Excel file.", 0);
                        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                        return new((T)(object)bytes, null, status);
                    }
                    var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
                    return new(value, value is null ? "The report was empty." : null, status);
                }
                catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
                { return new(default, "Could not load the report. Check your connection and try again.", 0); }
            }, retryUnauthorized: true);
        }
        catch (JSException) { return new(default, "Your browser session could not be checked. Reload and try again.", 0); }
    }
}
