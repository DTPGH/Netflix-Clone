using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Catalog;
using NetflixClone.Application.Catalog.Movies;
namespace NetflixClone.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/movies")]
public sealed class MoviesController(IBrowseMoviesUseCase browse) : ControllerBase
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

}
