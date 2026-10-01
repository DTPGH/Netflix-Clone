using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Admin.Movies;
using NetflixClone.Application.Admin.Movies;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;

namespace NetflixClone.Api.Controllers;

[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/admin/movies")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminMoviesController(IAdminMovieManagementUseCase useCase) : ControllerBase
{
    [HttpPost("people")]
    public async Task<IActionResult> CreatePerson(CreateAdminPersonRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await useCase.CreatePersonAsync(new(accountId, request.FullName, request.PhotoUrl, request.BirthDate), cancellationToken);
        if (result.IsFailure) return Failure(result.Error!);
        var person = result.Value!;
        return StatusCode(StatusCodes.Status201Created,
            new AdminPersonDetailResponse(person.PersonId, person.FullName, person.PhotoUrl, person.BirthDate));
    }
    [HttpGet("people")]
    public async Task<IActionResult> People([FromQuery] string? search, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await useCase.SearchPeopleAsync(accountId, search, cancellationToken);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value!.Select(p => new AdminPersonResponse(p.PersonId, p.FullName)));
    }
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ListAdminMoviesRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await useCase.ListAsync(new(accountId, request.Page, request.PageSize, request.Search,
            request.GenreId, request.IsAvailable, request.Deletion, request.Sort), cancellationToken);
        if (result.IsFailure) return Failure(result.Error!);
        var page = result.Value!;
        return Ok(new AdminMoviePageResponse(page.Items.Select(AdminMovieSummaryResponse.From).ToArray(),
            page.Page, page.PageSize, page.TotalCount));
    }

    [HttpGet("{movieId:int:min(1)}")]
    public async Task<IActionResult> Get(int movieId, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await useCase.GetAsync(new(accountId, movieId), cancellationToken);
        return result.IsFailure ? Failure(result.Error!) : Ok(AdminMovieDetailResponse.From(result.Value!));
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveAdminMovieRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await useCase.CreateAsync(new(accountId, Data(request)), cancellationToken);
        if (result.IsFailure) return Failure(result.Error!);
        var response = AdminMovieDetailResponse.From(result.Value!);
        return CreatedAtAction(nameof(Get), new { movieId = response.MovieId }, response);
    }

    [HttpPut("{movieId:int:min(1)}")]
    public async Task<IActionResult> Update(int movieId, UpdateAdminMovieRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await useCase.UpdateAsync(new(accountId, movieId, Data(request), request.ExpectedUpdatedAtUtc),
            cancellationToken);
        return result.IsFailure ? Failure(result.Error!) : Ok(AdminMovieDetailResponse.From(result.Value!));
    }

    [HttpDelete("{movieId:int:min(1)}")]
    public async Task<IActionResult> Delete(int movieId, [FromQuery] DateTime? expectedUpdatedAtUtc,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await useCase.DeleteAsync(new(accountId, movieId, expectedUpdatedAtUtc), cancellationToken);
        return result.IsFailure ? Failure(result.Error!) : NoContent();
    }

    [HttpPost("{movieId:int:min(1)}/restore")]
    public async Task<IActionResult> Restore(int movieId, RestoreAdminMovieRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(out var accountId)) return Unauthorized();
        var result = await useCase.RestoreAsync(new(accountId, movieId, request.ExpectedUpdatedAtUtc), cancellationToken);
        return result.IsFailure ? Failure(result.Error!) : Ok(AdminMovieDetailResponse.From(result.Value!));
    }

    private static SaveAdminMovieData Data(SaveAdminMovieRequest request) => new(request.Title,
        request.Description, request.ReleaseDate, request.DurationSeconds, request.ThumbnailUrl,
        request.BackdropUrl, request.TrailerUrl, request.VideoUrl, request.MaturityRating,
        request.IsFeatured, request.IsAvailable, request.GenreIds,
        request.Credits?.Select(c => c is null ? new SaveAdminMovieCredit(0, null, null) :
            new SaveAdminMovieCredit(c.PersonId, c.CreditType, c.CharacterName)).ToArray());

    private bool TryGetAccountId(out int accountId)
        => int.TryParse(User.FindFirst("sub")?.Value, out accountId) && accountId > 0;

    private IActionResult Failure(Error error) => error.Type switch
    {
        ErrorType.Validation => BadRequest(new { error.Code, error.Description }),
        ErrorType.Unauthorized => Unauthorized(new { error.Code, error.Description }),
        ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error.Code, error.Description }),
        ErrorType.NotFound => NotFound(new { error.Code, error.Description }),
        ErrorType.Conflict => Conflict(new { error.Code, error.Description }),
        _ => Problem(statusCode: 500, title: "An unexpected error occurred.")
    };
}
