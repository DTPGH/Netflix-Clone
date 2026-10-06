using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Application.Authentication.Devices;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Api.Controllers;
[ApiController, Authorize, Route("api/auth/devices")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountSessionsController(IRevokeAllDevicesUseCase useCase) : ControllerBase
{
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAll(CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var accountId) || accountId <= 0) return Unauthorized();
        var result = await useCase.ExecuteAsync(accountId, ct);
        return result.IsSuccess ? NoContent() : StatusCode(result.Error!.Type == ErrorType.Forbidden ? 403 : 409,
            new { result.Error.Code, result.Error.Description });
    }
}
