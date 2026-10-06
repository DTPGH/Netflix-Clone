using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Ratings;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.Ratings;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/profiles/{profileId:int:min(1)}/ratings/{movieId:int:min(1)}")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[NetflixClone.Api.Security.RequireProfileAccess]
public sealed class RatingsController(IGetRatingUseCase get, ISetRatingUseCase set, IRemoveRatingUseCase remove) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int profileId, int movieId, CancellationToken ct)
    {
        if (!AccountId(out var accountId)) return Unauthorized();
        var result = await get.ExecuteAsync(new(accountId, profileId, movieId), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(new RatingResponse(result.Value!.MovieId, result.Value.Value));
    }
    [HttpPut]
    public async Task<IActionResult> Set(int profileId, int movieId, SetRatingRequest request, CancellationToken ct)
    {
        if (!AccountId(out var accountId)) return Unauthorized();
        var result = await set.ExecuteAsync(new(accountId, profileId, movieId, request.Value), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(new RatingResponse(result.Value!.MovieId, result.Value.Value));
    }
    [HttpDelete]
    public async Task<IActionResult> Remove(int profileId, int movieId, CancellationToken ct)
    {
        if (!AccountId(out var accountId)) return Unauthorized();
        var result = await remove.ExecuteAsync(new(accountId, profileId, movieId), ct);
        return result.IsFailure ? Failure(result.Error!) : NoContent();
    }
    private bool AccountId(out int id) => int.TryParse(User.FindFirst("sub")?.Value, out id) && id > 0;
    private IActionResult Failure(Error error) => error.Type switch
    {
        ErrorType.NotFound => NotFound(new { error.Code, error.Description }),
        ErrorType.Validation => BadRequest(new { error.Code, error.Description }),
        ErrorType.Conflict => Conflict(new { error.Code, error.Description }),
        _ => Problem(statusCode: 500, title: "An unexpected error occurred.")
    };
}
