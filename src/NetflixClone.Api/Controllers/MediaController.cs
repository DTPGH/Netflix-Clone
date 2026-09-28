using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Media;
using NetflixClone.Api.Security;
using NetflixClone.Application.Catalog.Movies;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize(AuthenticationSchemes = PlaybackTicketHandler.SchemeName)]
[Route("api/media/{movieId:int:min(1)}")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class MediaController(IGetMoviePlaybackUseCase playback, PrivateDemoMedia media) : ControllerBase
{
    [HttpGet]
    [HttpHead]
    public async Task<IActionResult> Content(int movieId, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var account) ||
            !int.TryParse(User.FindFirst("profile")?.Value, out var profile) ||
            !int.TryParse(User.FindFirst("movie")?.Value, out var ticketMovie) || movieId != ticketMovie)
            return Unauthorized();
        var result = await playback.ExecuteAsync(new(account, profile, movieId), ct);
        if (result.IsFailure || result.Value!.MediaKey != User.FindFirst("media")?.Value) return NotFound();
        var path = media.Resolve(result.Value.MediaKey);
        if (path is null) return NotFound();
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(path, "video/mp4", enableRangeProcessing: true);
    }
}
