using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.WebAssembly.Http;
using NetflixClone.Web.Client.Models;

namespace NetflixClone.Web.Client.Services;

public sealed class AdminMediaUploadTransport(Uri apiBase) : IDisposable
{
    public HttpClient Client { get; } = new() { BaseAddress = apiBase, Timeout = TimeSpan.FromMinutes(30) };
    public void Dispose() => Client.Dispose();
}

public sealed class AdminMoviesApiClient(HttpClient http, AuthSession session, AdminMediaUploadTransport uploadTransport)
{
    public const long MaxImageBytes = 10 * 1024 * 1024;
    public Task<ApiResult<AdminPersonDetail>> CreatePersonAsync(CreateAdminPersonPayload payload, CancellationToken ct)
        => SendAuthenticatedAsync<AdminPersonDetail>(HttpMethod.Post, "api/admin/movies/people", payload, false, ct);
    public Task<ApiResult<AdminPerson[]>> SearchPeopleAsync(string search, CancellationToken ct)
        => SendAuthenticatedAsync<AdminPerson[]>(HttpMethod.Get, $"api/admin/movies/people?search={Uri.EscapeDataString(search)}", null, true, ct);
    public const long MaxVideoBytes = 1024L * 1024 * 1024;
    public Task<ApiResult<AdminMoviesReply>> ListAsync(int page, string search, int? genreId,
        bool? isAvailable, string deletion, CancellationToken ct) => SendAuthenticatedAsync<AdminMoviesReply>(
        HttpMethod.Get, $"api/admin/movies?page={page}&pageSize=20&search={Uri.EscapeDataString(search)}" +
        $"&deletion={Uri.EscapeDataString(deletion)}" + (genreId.HasValue ? $"&genreId={genreId}" : "") +
        (isAvailable.HasValue ? $"&isAvailable={isAvailable.Value.ToString().ToLowerInvariant()}" : ""),
        null, retry: true, ct);
    public Task<ApiResult<AdminMovieReply>> GetAsync(int movieId, CancellationToken ct)
        => SendAuthenticatedAsync<AdminMovieReply>(HttpMethod.Get, $"api/admin/movies/{movieId}", null, true, ct);
    public Task<ApiResult<AdminMovieReply>> CreateAsync(SaveAdminMoviePayload payload, CancellationToken ct)
        => SendAuthenticatedAsync<AdminMovieReply>(HttpMethod.Post, "api/admin/movies", payload, false, ct);
    public Task<ApiResult<AdminMovieReply>> UpdateAsync(int movieId, UpdateAdminMoviePayload payload, CancellationToken ct)
        => SendAuthenticatedAsync<AdminMovieReply>(HttpMethod.Put, $"api/admin/movies/{movieId}", payload, false, ct);
    public Task<ApiResult<object>> DeleteAsync(int movieId, DateTime version, CancellationToken ct)
        => SendAuthenticatedAsync<object>(HttpMethod.Delete,
            $"api/admin/movies/{movieId}?expectedUpdatedAtUtc={Uri.EscapeDataString(version.ToString("O"))}", null, false, ct);
    public Task<ApiResult<AdminMovieReply>> RestoreAsync(int movieId, DateTime version, CancellationToken ct)
        => SendAuthenticatedAsync<AdminMovieReply>(HttpMethod.Post, $"api/admin/movies/{movieId}/restore",
            new { ExpectedUpdatedAtUtc = version }, false, ct);
    public Task<ApiResult<AdminMediaUploadReply>> UploadImageAsync(IBrowserFile file, CancellationToken ct)
        => UploadAsync(file, "api/admin/media/images", MaxImageBytes, ct);
    public Task<ApiResult<AdminMediaUploadReply>> UploadVideoAsync(IBrowserFile file, CancellationToken ct)
        => UploadAsync(file, "api/admin/media/videos", MaxVideoBytes, ct);

    private async Task<ApiResult<AdminMediaUploadReply>> UploadAsync(IBrowserFile file, string path,
        long maxBytes, CancellationToken ct)
    {
        try
        {
            if (file.Size <= 0 || file.Size > maxBytes)
                return new(default, maxBytes == MaxImageBytes ? "Choose an image no larger than 10 MB."
                    : "Choose an MP4 no larger than 1 GB.", 400);
            await using var stream = file.OpenReadStream(maxBytes, ct);
            return await session.SendAuthenticatedAsync<AdminMediaUploadReply>(async token =>
            {
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, path);
                    request.SetBrowserRequestStreamingEnabled(true);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    using var multipart = new MultipartFormDataContent();
                    using var fileContent = new StreamContent(stream);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType)
                        ? mediaType : new MediaTypeHeaderValue("application/octet-stream");
                    multipart.Add(fileContent, "file", file.Name);
                    request.Content = multipart;
                    using var response = await uploadTransport.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                    if (!response.IsSuccessStatusCode)
                        return new(default, (int)response.StatusCode switch
                        {
                            400 or 413 => "The selected file type, content or size is not supported.",
                            401 => "Please sign in again.",
                            403 => "Your account does not currently have administrator access.",
                            _ => "The upload could not be confirmed. Choose the file again before retrying."
                        }, (int)response.StatusCode);
                    var value = await response.Content.ReadFromJsonAsync<AdminMediaUploadReply>(cancellationToken: ct);
                    return value is null ? new(default, "The upload response was empty. Choose the file again.", 0)
                        : new(value, null, (int)response.StatusCode);
                }
                catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException or IOException)
                {
                    return new(default, "The upload could not be confirmed. Choose the file again before retrying.", 0);
                }
            }, retryUnauthorized: false);
        }
        catch (Exception exception) when (exception is IOException or JSException or ArgumentException)
        {
            return new(default, "The browser could not read this file. Choose it again and retry.", 0);
        }
    }

    private async Task<ApiResult<T>> SendAuthenticatedAsync<T>(HttpMethod method, string path, object? body,
        bool retry, CancellationToken ct)
    {
        try
        {
            return await session.SendAuthenticatedAsync(token => SendAsync<T>(method, path, body, token, ct), retry);
        }
        catch (JSException)
        {
            return new(default, "Your browser session could not be checked. Reload the page and try again.", 0);
        }
    }

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body, string token,
        CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null) request.Content = JsonContent.Create(body);
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                string? code = null, description = null;
                try
                {
                    using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                    if (json.RootElement.TryGetProperty("code", out var codeValue)) code = codeValue.GetString();
                    if (json.RootElement.TryGetProperty("description", out var descriptionValue)) description = descriptionValue.GetString();
                }
                catch (JsonException) { }
                var message = (int)response.StatusCode switch
                {
                    400 => description ?? "Check the movie information and try again.",
                    401 => "Please sign in again.",
                    403 => "Your account does not currently have administrator access.",
                    404 => "This movie no longer exists. Reload the list.",
                    409 => description ?? "This movie changed. Reload it before trying again.",
                    _ => "The server could not confirm the result. Reload before trying again."
                };
                return new(default, message, (int)response.StatusCode, code);
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return new(default, null, 204);
            var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return result is null ? new(default, "The server returned an empty response. Reload before trying again.", 0)
                : new(result, null, (int)response.StatusCode);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(default, "The result could not be confirmed. Check your connection and reload before trying again.", 0);
        }
    }
}
