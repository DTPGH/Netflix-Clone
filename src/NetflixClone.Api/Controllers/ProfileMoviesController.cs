using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Catalog;
using NetflixClone.Application.Catalog.Movies;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/profiles/{profileId:int:min(1)}/movies")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[NetflixClone.Api.Security.RequireProfileAccess]
public sealed class ProfileMoviesController(IBrowseMoviesUseCase browse, IGetMovieDetailUseCase detail) : ControllerBase
{
    [HttpGet("{movieId:int:min(1)}")]
    public async Task<IActionResult> Detail(int profileId, int movieId, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId <= 0) return Unauthorized();
        var result = await detail.ExecuteAsync(new(accountId, profileId, movieId), ct);
        return result.IsFailure ? NotFound(new { result.Error!.Code, result.Error.Description })
            : Ok(MovieDetailResponse.From(result.Value!));
    }
    [HttpGet]
    public async Task<IActionResult> Browse(int profileId, [FromQuery] BrowseMoviesRequest request, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId <= 0) return Unauthorized();
        var result = await browse.ExecuteAsync(new(request.Page, request.PageSize, request.Search,
            request.GenreId, request.Sort, accountId, profileId), ct);
        if (result.IsFailure)
            return result.Error!.Type == ErrorType.NotFound
                ? NotFound(new { result.Error.Code, result.Error.Description })
                : BadRequest(new { result.Error.Code, result.Error.Description });
        var value = result.Value!;
        return Ok(new BrowseMoviesResponse(value.Items.Select(MovieSummaryResponse.From).ToArray(),
            value.Page, value.PageSize, value.TotalCount));
    }
}
