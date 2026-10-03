using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Application.Admin.Reports;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
namespace NetflixClone.Api.Controllers;
[ApiController, Authorize(Roles = RoleNames.Admin)]
[Route("api/admin/reports/views")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AdminViewingReportsController(IAdminViewingReportUseCase useCase) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.GetAsync(id, new(fromUtc, toUtc), page, pageSize, ct);
        return result.IsFailure ? Failure(result.Error!) : Ok(result.Value);
    }
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateTime fromUtc, [FromQuery] DateTime toUtc, CancellationToken ct)
    {
        if (!Actor(out var id)) return Unauthorized();
        var result = await useCase.ExportAsync(id, new(fromUtc, toUtc), ct);
        return result.IsFailure ? Failure(result.Error!) : File(result.Value!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"viewing-report-{fromUtc:yyyyMMdd}-{toUtc:yyyyMMdd}.xlsx");
    }
    private bool Actor(out int id) => int.TryParse(User.FindFirst("sub")?.Value, out id) && id > 0;
    private IActionResult Failure(Error e) => StatusCode(e.Type switch { ErrorType.Validation => 400, ErrorType.Forbidden => 403, _ => 500 }, new { e.Code, e.Description });
}
