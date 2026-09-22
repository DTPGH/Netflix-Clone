using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Catalog;
using NetflixClone.Application.Catalog.Genres;
namespace NetflixClone.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/genres")]
public sealed class GenresController(IListGenresUseCase list) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await list.ExecuteAsync(cancellationToken);
        if (result.IsFailure) return Problem(statusCode: 500, title: "An unexpected error occurred.");
        return Ok(new GenresResponse(result.Value!.Select(g => new GenreResponse(g.GenreId, g.Name)).ToArray()));
    }
}
