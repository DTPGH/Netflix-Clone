using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Components;

namespace NetflixClone.Web.Client.Services;

// Shared by API clients, including coordinated access-token retries.
// Depends on grant storage alone to avoid HttpClient/AuthSession/ActiveProfileState DI cycles.
public sealed class ProfileAccessHandler(ProfileAccessState state, NavigationManager navigation, Uri apiOrigin) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var uri = request.RequestUri!;
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var profileId = uri.Scheme == apiOrigin.Scheme && uri.Authority == apiOrigin.Authority &&
            segments.Length >= 4 && segments[0] == "api" && segments[1] == "profiles" &&
            int.TryParse(segments[2], out var id) && segments[3] is not ("pin" or "unlock") ? id : 0;
        var version = state.Version;
        if (profileId > 0 && state.GetToken(profileId) is { } token)
            request.Headers.Add("X-Profile-Unlock", token);
        var response = await base.SendAsync(request, ct);
        if (profileId > 0 && response.StatusCode == HttpStatusCode.Forbidden)
        {
            try
            {
                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                if (json.RootElement.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String &&
                    code.GetString() == "Profiles.UnlockRequired" &&
                    state.RequireUnlock(profileId, version))
                {
                    var current = "/" + navigation.ToBaseRelativePath(navigation.Uri).Split('?')[0];
                    navigation.NavigateTo("/select-profile?returnTo=" + Uri.EscapeDataString(ProfileNavigation.Destination(current)));
                }
            }
            catch (JsonException) { }
        }
        return response;
    }
}
