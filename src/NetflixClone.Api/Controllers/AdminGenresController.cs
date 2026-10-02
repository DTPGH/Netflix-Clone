using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Admin.Genres;
using NetflixClone.Application.Admin.Genres;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/admin/genres")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminGenresController(IAdminGenreUseCase useCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.ListAsync(id, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value!.Select(Map));
    }
    [HttpPost]
    public async Task<IActionResult> Create(SaveGenreRequest request, CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.SaveAsync(new(id, null, request.Name, null), ct);
        return result.IsFailure ? Failure(result.Error!) : StatusCode(201, Map(result.Value!));
    }
    [HttpPut("{genreId:int:min(1)}")]
    public async Task<IActionResult> Update(int genreId, SaveGenreRequest request, CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.SaveAsync(new(id, genreId, request.Name, request.ExpectedName), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(Map(result.Value!));
    }
    [HttpDelete("{genreId:int:min(1)}")]
    public async Task<IActionResult> Delete(int genreId, [FromQuery] string? expectedName, CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.DeleteAsync(id, genreId, expectedName, ct);
        return result.IsFailure ? Failure(result.Error!) : NoContent();
    }
    private bool Actor(out int id) => int.TryParse(User.FindFirst("sub")?.Value, out id) && id > 0;
    private static AdminGenreResponse Map(AdminGenre g) => new(g.GenreId, g.Name, g.MovieCount);
    private IActionResult Failure(Error e) => StatusCode(e.Type switch { ErrorType.Validation => 400, ErrorType.Forbidden => 403,
        ErrorType.NotFound => 404, ErrorType.Conflict => 409, _ => 500 }, new { e.Code, e.Description });
}
