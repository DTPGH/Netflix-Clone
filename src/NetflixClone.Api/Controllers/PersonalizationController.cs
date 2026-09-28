using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Catalog;
using NetflixClone.Api.Contracts.Personalization;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.Personalization;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/profiles/{profileId:int:min(1)}")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PersonalizationController(IGetOnboardingUseCase get, IGetOnboardingMoviesUseCase movies,
    ICompleteOnboardingUseCase complete, IGetRecommendationsUseCase recommendations) : ControllerBase
{
    [HttpGet("onboarding")]
    public async Task<IActionResult> Get(int profileId, CancellationToken ct)
    {
        if (!AccountId(out var account)) return Unauthorized();
        var result = await get.ExecuteAsync(new(account, profileId), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(new OnboardingResponse(result.Value!.OnboardingCompleted, result.Value.MovieIds));
    }
    [HttpGet("onboarding/movies")]
    public async Task<IActionResult> Movies(int profileId, [FromQuery] OnboardingMoviesRequest request, CancellationToken ct)
    {
        if (!AccountId(out var account)) return Unauthorized();
        var result = await movies.ExecuteAsync(new(account, profileId, request.Page, request.PageSize, request.Search), ct);
        if (result.IsFailure) return Failure(result.Error!);
        var value = result.Value!;
        return Ok(new BrowseMoviesResponse(value.Items.Select(MovieSummaryResponse.From).ToArray(), value.Page, value.PageSize, value.TotalCount));
    }
    [HttpPut("onboarding")]
    public async Task<IActionResult> Complete(int profileId, CompleteOnboardingRequest request, CancellationToken ct)
    {
        if (!AccountId(out var account)) return Unauthorized();
        var result = await complete.ExecuteAsync(new(account, profileId, request.MovieIds), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(new OnboardingResponse(result.Value!.OnboardingCompleted, result.Value.MovieIds));
    }
    [HttpGet("recommendations")]
    public async Task<IActionResult> Recommendations(int profileId, [FromQuery] int limit = 12, CancellationToken ct = default)
    {
        if (!AccountId(out var account)) return Unauthorized();
        var result = await recommendations.ExecuteAsync(new(account, profileId, limit), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(new RecommendationsResponse(
            result.Value!.Items.Select(MovieSummaryResponse.From).ToArray(), result.Value.Source));
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
