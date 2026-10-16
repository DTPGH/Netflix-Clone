using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Catalog;
using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Api.Media;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/profiles/{profileId:int:min(1)}/movies")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[NetflixClone.Api.Security.RequireProfileAccess]
public sealed class PlaybackController(IGetMoviePlaybackUseCase playback, IPlaybackTicketService tickets, PrivateDemoMedia media,
    NetflixClone.Application.Viewing.IViewingSessionUseCase sessions) : ControllerBase
{
    [HttpPost("{movieId:int:min(1)}/playback")]
    public async Task<IActionResult> Start(int profileId, int movieId, StartPlaybackRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId <= 0) return Unauthorized();
        var result = await playback.ExecuteAsync(new(accountId, profileId, movieId), cancellationToken);
        if (result.IsFailure)
            return StatusCode(result.Error!.Type switch { ErrorType.NotFound => 404, ErrorType.Forbidden => 403, _ => 409 },
                new { result.Error.Code, result.Error.Description });
        var value = result.Value!;
        if (media.Resolve(value.MediaKey) is null)
            return Conflict(new { Code = "Movies.PlaybackUnavailable", Description = "Video is not available." });
        var reservation = await sessions.StartAsync(accountId, profileId, movieId, request.DeviceIdentifier, request.ClientSessionId, cancellationToken);
        if (reservation.IsFailure)
            return StatusCode(reservation.Error!.Type switch { ErrorType.NotFound => 404, ErrorType.Validation => 400,
                ErrorType.Forbidden => 403, ErrorType.Conflict => 409, _ => 500 }, new { reservation.Error.Code, reservation.Error.Description });
        if (reservation.Value!.IsEnded)
            return Conflict(new { NetflixClone.Application.Viewing.ViewingSessionUseCase.SessionExpired.Code,
                NetflixClone.Application.Viewing.ViewingSessionUseCase.SessionExpired.Description });
        var ticket = tickets.Issue(accountId, profileId, movieId, value.MediaKey, reservation.Value.SessionId,
            Request.Headers[NetflixClone.Api.Security.ProfileAccessFilter.HeaderName].FirstOrDefault());
        var url = $"{Request.PathBase}/api/media/{movieId}?ticket={Uri.EscapeDataString(ticket.Token)}";
        return Ok(new PlaybackResponse(value.MovieId, value.Title, url, value.ContentType, value.IsDemo, ticket.ExpiresAtUtc, reservation.Value));
    }
}
