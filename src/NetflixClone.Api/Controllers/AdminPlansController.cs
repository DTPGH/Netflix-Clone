using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Admin.Plans;
using NetflixClone.Application.Admin.Plans;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
namespace NetflixClone.Api.Controllers;
[ApiController]
[Authorize(Roles = RoleNames.Admin)]
[Route("api/admin/plans")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminPlansController(IAdminPlanUseCase useCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.ListAsync(id, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value!.Select(PlanResponse.From));
    }
    [HttpGet("{planId:int:min(1)}")]
    public async Task<IActionResult> Get(int planId, CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.GetAsync(id, planId, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(PlanResponse.From(result.Value!));
    }
    [HttpPost]
    public async Task<IActionResult> Create(SavePlanRequest request, CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.SaveAsync(new(id, null, request.Name, request.Price, request.MaxConcurrentStreams, request.MaxQuality, null), ct);
        return result.IsFailure ? Failure(result.Error!) : CreatedAtAction(nameof(Get), new { planId = result.Value!.PlanId }, PlanResponse.From(result.Value!));
    }
    [HttpPut("{planId:int:min(1)}")]
    public async Task<IActionResult> Update(int planId, SavePlanRequest request, CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.SaveAsync(new(id, planId, request.Name, request.Price, request.MaxConcurrentStreams, request.MaxQuality, request.ExpectedUpdatedAtUtc), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(PlanResponse.From(result.Value!));
    }
    [HttpPut("{planId:int:min(1)}/status")]
    public async Task<IActionResult> Status(int planId, PlanStatusRequest request, CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.StatusAsync(id, planId, request.IsActive, request.ExpectedUpdatedAtUtc, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(PlanResponse.From(result.Value!));
    }
    private bool Actor(out int id) => int.TryParse(User.FindFirst("sub")?.Value, out id) && id > 0;
    private IActionResult Failure(Error e) => StatusCode(e.Type switch { ErrorType.Validation => 400, ErrorType.Forbidden => 403,
        ErrorType.NotFound => 404, ErrorType.Conflict => 409, _ => 500 }, new { e.Code, e.Description });
}
