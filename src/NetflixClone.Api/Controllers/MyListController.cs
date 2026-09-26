using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Catalog;
using NetflixClone.Api.Contracts.MyList;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.MyList;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/profiles/{profileId:int:min(1)}/my-list")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class MyListController(IListMyListUseCase list, IGetMyListStatusUseCase status,
    IAddToMyListUseCase add, IRemoveFromMyListUseCase remove) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int profileId, [FromQuery] MyListPageRequest request, CancellationToken ct)
    {
        if (!AccountId(out var accountId)) return Unauthorized();
        var result = await list.ExecuteAsync(new(accountId, profileId, request.Page, request.PageSize), ct);
        if (result.IsFailure) return Failure(result.Error!);
        var value = result.Value!;
        return Ok(new MyListResponse(value.Items.Select(MovieSummaryResponse.From).ToArray(), value.Page, value.PageSize, value.TotalCount));
    }
    [HttpGet("{movieId:int:min(1)}")]
    public async Task<IActionResult> Status(int profileId, int movieId, CancellationToken ct)
    {
        if (!AccountId(out var accountId)) return Unauthorized();
        var result = await status.ExecuteAsync(new(accountId, profileId, movieId), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(new MyListStatusResponse(result.Value!.IsInMyList));
    }
    [HttpPut("{movieId:int:min(1)}")]
    public async Task<IActionResult> Add(int profileId, int movieId, CancellationToken ct)
    {
        if (!AccountId(out var accountId)) return Unauthorized();
        var result = await add.ExecuteAsync(new(accountId, profileId, movieId), ct);
        return result.IsFailure ? Failure(result.Error!) : NoContent();
    }
    [HttpDelete("{movieId:int:min(1)}")]
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
        _ => Problem(statusCode: 500, title: "An unexpected error occurred.")
    };
}
