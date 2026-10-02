using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Collections;
using NetflixClone.Api.Contracts.Catalog;
using NetflixClone.Application.Collections;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/admin/movie-collections")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminMovieCollectionsController(IMovieCollectionUseCase useCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!Account(out var id)) return Unauthorized();
        var result = await useCase.ListAsync(id, page, pageSize, ct);
        if (result.IsFailure) return Failure(result.Error!);
        var p = result.Value!;
        return Ok(new CollectionPageResponse(p.Items.Select(c => new CollectionSummaryResponse(c.CollectionId, c.Title,
            c.IsPublished, c.DisplayOrder, c.MovieCount, c.UpdatedAtUtc)).ToArray(), p.Page, p.PageSize, p.TotalCount));
    }
    [HttpGet("{collectionId:int:min(1)}")]
    public async Task<IActionResult> Get(int collectionId, CancellationToken ct)
    {
        if (!Account(out var id)) return Unauthorized();
        var result = await useCase.GetAsync(id, collectionId, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(Map(result.Value!));
    }
    [HttpPost]
    public async Task<IActionResult> Create(SaveCollectionRequest request, CancellationToken ct)
    {
        if (!Account(out var id)) return Unauthorized();
        var result = await useCase.SaveAsync(new(id, null, request.Title, request.IsPublished, request.DisplayOrder, request.MovieIds, null), ct);
        return result.IsFailure ? Failure(result.Error!) : CreatedAtAction(nameof(Get), new { collectionId = result.Value!.CollectionId }, Map(result.Value!));
    }
    [HttpPut("{collectionId:int:min(1)}")]
    public async Task<IActionResult> Update(int collectionId, SaveCollectionRequest request, CancellationToken ct)
    {
        if (!Account(out var id)) return Unauthorized();
        var result = await useCase.SaveAsync(new(id, collectionId, request.Title, request.IsPublished, request.DisplayOrder, request.MovieIds, request.ExpectedUpdatedAtUtc), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(Map(result.Value!));
    }
    private bool Account(out int id) => int.TryParse(User.FindFirst("sub")?.Value, out id) && id > 0;
    private static CollectionDetailResponse Map(CollectionDetail c) => new(c.CollectionId, c.Title, c.IsPublished, c.DisplayOrder,
        c.UpdatedAtUtc, c.Movies.Select(m => new CollectionMovieResponse(m.MovieId, m.Title, m.ThumbnailUrl, m.IsDeleted)).ToArray());
    private IActionResult Failure(Error e) => StatusCode(e.Type switch { ErrorType.Validation => 400, ErrorType.Unauthorized => 401,
        ErrorType.Forbidden => 403, ErrorType.NotFound => 404, ErrorType.Conflict => 409, _ => 500 }, new { e.Code, e.Description });
}
[ApiController]
[Authorize]
[Route("api/profiles/{profileId:int:min(1)}/movie-collections")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ProfileMovieCollectionsController(IMovieCollectionUseCase useCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(int profileId, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var id) || id <= 0) return Unauthorized();
        var result = await useCase.BrowseAsync(id, profileId, ct);
        return result.IsFailure ? NotFound(new { result.Error!.Code, result.Error.Description })
            : Ok(result.Value!.Select(c => new BrowseCollectionResponse(c.CollectionId, c.Title, c.Movies.Select(MovieSummaryResponse.From).ToArray())));
    }
}
