using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Application.Admin.Dashboard;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
namespace NetflixClone.Api.Controllers;
[ApiController, Authorize(Roles = RoleNames.Admin)]
[Route("api/admin/dashboard")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminDashboardController(IAdminDashboardUseCase useCase, IClock clock) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly? from, [FromQuery] DateOnly? through, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var actor) || actor <= 0) return Unauthorized();
        var today = DateOnly.FromDateTime(clock.UtcNow.AddHours(7));
        var result = await useCase.GetAsync(actor, new(from ?? today.AddDays(-29), through ?? today), ct);
        return result.IsSuccess ? Ok(result.Value) : StatusCode(result.Error!.Type == ErrorType.Forbidden ? 403 : 400,
            new { result.Error.Code, result.Error.Description });
    }
}
