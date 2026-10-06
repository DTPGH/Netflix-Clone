using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NetflixClone.Application.Common.Abstractions.Security;
namespace NetflixClone.Api.Security;
public sealed class PlaybackTicketHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
    UrlEncoder encoder, IPlaybackTicketService tickets) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "PlaybackTicket";
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Path.StartsWithSegments("/api/media")) return Task.FromResult(AuthenticateResult.NoResult());
        var values = Request.Query["ticket"];
        var data = values.Count == 1 ? tickets.Validate(values[0] ?? "") : null;
        if (data is null) return Task.FromResult(AuthenticateResult.Fail("Invalid playback ticket."));
        var identity = new ClaimsIdentity(new[] {
            new Claim("sub", data.UserAccountId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim("profile", data.ProfileId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim("movie", data.MovieId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim("media", data.MediaKey)
        }, SchemeName);
        if (data.ProfileUnlockToken is not null) identity.AddClaim(new Claim("profile_unlock", data.ProfileUnlockToken));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
