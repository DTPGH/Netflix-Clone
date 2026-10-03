using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Subscriptions;
using NetflixClone.Application.Subscriptions;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Api.Controllers;
[ApiController, Authorize]
[Route("api/subscriptions")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SubscriptionsController(ISubscriptionUseCase useCase) : ControllerBase
{
    [HttpGet("plans")]
    public async Task<IActionResult> Plans(CancellationToken ct)
    {
        if (!Account(out var id)) return Unauthorized();
        var result = await useCase.PlansAsync(id, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value);
    }
    [HttpGet("current")]
    public async Task<IActionResult> Current(CancellationToken ct)
    {
        if (!Account(out var id)) return Unauthorized();
        var result = await useCase.CurrentAsync(id, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value);
    }
    [HttpGet("payments")]
    public async Task<IActionResult> Payments([FromQuery] int page = 1, CancellationToken ct = default)
    {
        if (!Account(out var id)) return Unauthorized();
        var result = await useCase.PaymentsAsync(id, page, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value);
    }
    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase(PurchaseSubscriptionRequest request, CancellationToken ct)
    {
        if (!Account(out var id)) return Unauthorized();
        var result = await useCase.PurchaseAsync(new(id, request.PlanId, request.IdempotencyKey, request.ExpectedPlanUpdatedAtUtc), ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value);
    }
    private bool Account(out int id) => int.TryParse(User.FindFirst("sub")?.Value, out id) && id > 0;
    private IActionResult Failure(Error error) => error.Type == ErrorType.Failure
        ? Problem(statusCode: 500, title: error.Description)
        : StatusCode(error.Type switch { ErrorType.Forbidden => 403, ErrorType.Conflict => 409, ErrorType.Validation => 400, _ => 500 }, new { error.Code, error.Description });
}
