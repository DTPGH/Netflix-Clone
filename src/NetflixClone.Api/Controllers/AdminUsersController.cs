using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Admin.Users;
using NetflixClone.Application.Admin.Users;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/admin/users")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminUsersController(IAdminUserUseCase useCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null, [FromQuery] bool? isLocked = null, CancellationToken ct = default)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.ListAsync(id, new(page, pageSize, search, isLocked), ct);
        if (result.IsFailure) return Failure(result.Error!);
        var p = result.Value!;
        return Ok(new UserPageResponse(p.Items.Select(UserResponse.From).ToArray(), p.Page, p.PageSize, p.TotalCount));
    }
    [HttpGet("roles")]
    public async Task<IActionResult> Roles(CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.RolesAsync(id, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value!.Select(r => new RoleResponse(r.RoleId, r.Name)));
    }
    [HttpGet("{userId:int:min(1)}")]
    public async Task<IActionResult> Get(int userId, CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.GetAsync(id, userId, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(UserResponse.From(result.Value!));
    }
    [HttpPut("{userId:int:min(1)}/lock")]
    public async Task<IActionResult> Lock(int userId, LockUserRequest request, CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.LockAsync(new(id, userId, request.IsLocked, request.Reason, request.ExpectedUpdatedAtUtc), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(UserResponse.From(result.Value!));
    }
    [HttpPut("{userId:int:min(1)}/roles")]
    public async Task<IActionResult> ChangeRoles(int userId, ChangeRolesRequest request, CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.ChangeRolesAsync(new(id, userId, request.RoleIds, request.Reason, request.ExpectedUpdatedAtUtc), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(UserResponse.From(result.Value!));
    }
    [HttpGet("/api/admin/action-logs")]
    public async Task<IActionResult> Logs([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? actorId = null,
        [FromQuery] int? targetId = null, [FromQuery] string? action = null, CancellationToken ct = default)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.LogsAsync(id, new(page, pageSize, actorId, targetId, action), ct);
        if (result.IsFailure) return Failure(result.Error!);
        var p = result.Value!;
        return Ok(new LogPageResponse(p.Items.Select(l => new LogResponse(l.LogId, l.ActorUserAccountId, l.ActorEmail,
            l.TargetUserAccountId, l.TargetEmail, l.Action, l.Reason, l.CreatedAtUtc)).ToArray(), p.Page, p.PageSize, p.TotalCount));
    }
    private bool Actor(out int id) => int.TryParse(User.FindFirst("sub")?.Value, out id) && id > 0;
    private IActionResult Failure(Error e) => StatusCode(e.Type switch { ErrorType.Validation => 400, ErrorType.Forbidden => 403,
        ErrorType.NotFound => 404, ErrorType.Conflict => 409, _ => 500 }, new { e.Code, e.Description });
}
