using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Catalog;
using NetflixClone.Application.Catalog.Movies;
namespace NetflixClone.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/movies")]
public sealed class MoviesController(IBrowseMoviesUseCase browse, IGetMovieDetailUseCase detail) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Browse([FromQuery] BrowseMoviesRequest request, CancellationToken cancellationToken)
    {
        var result = await browse.ExecuteAsync(new(request.Page, request.PageSize, request.Search, request.GenreId, request.Sort), cancellationToken);
        if (result.IsFailure) return BadRequest(new { result.Error!.Code, result.Error.Description });
        var value = result.Value!;
        return Ok(new BrowseMoviesResponse(value.Items.Select(MovieSummaryResponse.From).ToArray(),
            value.Page, value.PageSize, value.TotalCount));
    }

    [HttpGet("{movieId:int}")]
    public async Task<IActionResult> Detail(int movieId, CancellationToken cancellationToken)
    {
        var result = await detail.ExecuteAsync(movieId, cancellationToken);
        return result.IsFailure
            ? NotFound(new { result.Error!.Code, result.Error.Description })
            : Ok(MovieDetailResponse.From(result.Value!));
    }
}
