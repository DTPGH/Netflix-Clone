using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Catalog;
using NetflixClone.Api.Contracts.Viewing;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.Viewing;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/profiles/{profileId:int:min(1)}")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class WatchHistoryController(IWatchProgressUseCase useCase) : ControllerBase
{
    [HttpGet("watch-history")]
    public async Task<IActionResult> List(int profileId, [FromQuery] int page = 1, CancellationToken ct = default)
    {
        if (!Account(out var account)) return Unauthorized();
        var result = await useCase.ListAsync(account, profileId, page, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(new {
            Items = result.Value!.Items.Select(i => new {
                Movie = MovieSummaryResponse.From(i.Movie), i.PositionSeconds, i.IsCompleted, i.LastWatchedAtUtc
            }), result.Value.TotalCount
        });
    }
    [HttpGet("watch-history/{movieId:int:min(1)}")]
    public async Task<IActionResult> Get(int profileId, int movieId, CancellationToken ct)
    {
        if (!Account(out var account)) return Unauthorized();
        var result = await useCase.GetAsync(new(account, profileId, movieId), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value);
    }
    [HttpPut("watch-history/{movieId:int:min(1)}")]
    public async Task<IActionResult> Save(int profileId, int movieId, WatchProgressRequest request, CancellationToken ct)
    {
        if (!Account(out var account)) return Unauthorized();
        var result = await useCase.SaveAsync(new(account, profileId, movieId, request.PositionSeconds,
            request.DurationSeconds, request.Ended, request.ExpectedUpdatedAtUtc), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value);
    }
    [HttpGet("continue-watching")]
    public async Task<IActionResult> Continue(int profileId, [FromQuery] int limit = 12, CancellationToken ct = default)
    {
        if (!Account(out var account)) return Unauthorized();
        var result = await useCase.ContinueAsync(account, profileId, limit, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(new { Items = result.Value!.Select(i => new {
            Movie = MovieSummaryResponse.From(i.Movie), i.PositionSeconds, i.LastWatchedAtUtc }) });
    }
    private bool Account(out int account) => int.TryParse(User.FindFirst("sub")?.Value, out account) && account > 0;
    private IActionResult Failure(Error error) => error.Type switch
    {
        ErrorType.NotFound => NotFound(new { error.Code, error.Description }),
        ErrorType.Conflict => Conflict(new { error.Code, error.Description }),
        ErrorType.Validation => BadRequest(new { error.Code, error.Description }),
        _ => Problem(statusCode: 500, title: "An unexpected error occurred.")
    };
}
