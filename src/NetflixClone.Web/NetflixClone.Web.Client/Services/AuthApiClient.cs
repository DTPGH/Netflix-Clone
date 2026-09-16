using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using NetflixClone.Web.Client.Models;

namespace NetflixClone.Web.Client.Services;

public sealed class AuthApiClient(HttpClient http)
{
    public Task<ApiResult<RegisterReply>> RegisterAsync(RegisterForm form) => SendAsync<RegisterReply>(HttpMethod.Post, "register", form);
    public Task<ApiResult<object>> ConfirmAsync(ConfirmForm form) => SendAsync<object>(HttpMethod.Post, "confirm-email", form);
    public Task<ApiResult<object>> ResendAsync(ResendForm form) => SendAsync<object>(HttpMethod.Post, "resend-email-confirmation", form);
    public Task<ApiResult<LoginReply>> LoginAsync(LoginPayload form) => SendAsync<LoginReply>(HttpMethod.Post, "login", form);
    public Task<ApiResult<TokenReply>> RefreshAsync(string token) => SendAsync<TokenReply>(HttpMethod.Post, "refresh-token", new TokenPayload(token));
    public Task<ApiResult<object>> LogoutAsync(string token) => SendAsync<object>(HttpMethod.Post, "logout", new TokenPayload(token));
    public Task<ApiResult<MeReply>> MeAsync(string token) => SendAsync<MeReply>(HttpMethod.Get, "me", bearer: token);

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, object? body = null, string? bearer = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, "api/auth/" + path);
            if (body is not null) request.Content = JsonContent.Create(body);
            if (bearer is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            using var response = await http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                // Allowlisted messages only: never render arbitrary server exceptions or echoed input.
                string? code = null;
                try
                {
                    using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    if (error.RootElement.TryGetProperty("code", out var property)) code = property.GetString();
                }
                catch (JsonException) { }
                var message = code switch
                {
                    "Auth.Login.InvalidCredentials" => "The email or password is incorrect.",
                    "Auth.Login.AccountLocked" => "This account is locked. Please contact support.",
                    "Auth.Login.EmailNotConfirmed" => "Please confirm your email before signing in.",
                    "Auth.Login.TemporarilyLocked" => "Too many attempts. Please wait and try again later.",
                    "Auth.Register.EmailAlreadyExists" => "An account with this email already exists. Try signing in.",
                    "Auth.EmailConfirmation.InvalidOrExpiredToken" => "This confirmation code is invalid or expired. Request a new one.",
                    "Auth.RefreshToken.InvalidRefreshToken" => "Your session has ended. Please sign in again.",
                    _ when (int)response.StatusCode >= 500 => "The service is temporarily unavailable. Please try again.",
                    _ when response.StatusCode == System.Net.HttpStatusCode.Unauthorized => "Please sign in again.",
                    _ => "The request could not be completed. Check the form and try again."
                };
                return new(default, message, (int)response.StatusCode);
            }
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                return new(default, null, (int)response.StatusCode);
            var value = await response.Content.ReadFromJsonAsync<T>();
            return value is null ? new(default, "The service returned an incomplete response.", 0)
                : new(value, null, (int)response.StatusCode);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new(default, "Cannot reach the service. Check your connection and try again.", 0);
        }
    }
}

