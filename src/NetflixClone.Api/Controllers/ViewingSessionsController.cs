using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Viewing;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.Viewing;
namespace NetflixClone.Api.Controllers;
[ApiController, Authorize]
[Route("api/profiles/{profileId:int:min(1)}/movies/{movieId:int:min(1)}/viewing-sessions")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ViewingSessionsController(IViewingSessionUseCase useCase) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Start(int profileId, int movieId, StartViewingSessionRequest request, CancellationToken ct)
    {
        if (!Account(out var account)) return Unauthorized();
        var result = await useCase.StartAsync(account, profileId, movieId, request.DeviceIdentifier, request.ClientSessionId, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value);
    }
    // EndReason closes the session atomically with the final cumulative checkpoint.
    [HttpPut("{sessionId:int:min(1)}/progress")]
    public async Task<IActionResult> Checkpoint(int profileId, int movieId, int sessionId, ViewingCheckpointRequest request, CancellationToken ct)
    {
        if (!Account(out var account)) return Unauthorized();
        var result = await useCase.CheckpointAsync(account, profileId, movieId, request.DeviceIdentifier, sessionId,
            new(request.Sequence, request.WatchedMilliseconds, request.EndReason), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value);
    }
    private bool Account(out int account) => int.TryParse(User.FindFirst("sub")?.Value, out account) && account > 0;
    private IActionResult Failure(Error error) => error.Type switch
    {
        ErrorType.NotFound => NotFound(new { error.Code, error.Description }),
        ErrorType.Conflict => Conflict(new { error.Code, error.Description }),
        ErrorType.Validation => BadRequest(new { error.Code, error.Description }),
        _ => Problem(statusCode: 500, title: "Viewing session could not be saved.")
    };
}
