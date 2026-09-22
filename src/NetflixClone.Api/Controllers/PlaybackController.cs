using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Catalog;
using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/movies")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PlaybackController(IGetMoviePlaybackUseCase playback) : ControllerBase
{
    [HttpPost("{movieId:int:min(1)}/playback")]
    public async Task<IActionResult> Start(int movieId, CancellationToken cancellationToken)
    {
        var result = await playback.ExecuteAsync(movieId, cancellationToken);
        if (result.IsFailure)
            return result.Error!.Type == ErrorType.NotFound
                ? NotFound(new { result.Error.Code, result.Error.Description })
                : Conflict(new { result.Error.Code, result.Error.Description });
        var value = result.Value!;
        return Ok(new PlaybackResponse(value.MovieId, value.Title, value.VideoUrl, value.ContentType, value.IsDemo));
    }
}
