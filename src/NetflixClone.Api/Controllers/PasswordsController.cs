using NetflixClone.Api.Contracts.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Application.Authentication.Passwords;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Api.Controllers;
[ApiController, Route("api/auth"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PasswordsController(IPasswordUseCase useCase) : ControllerBase
{
    [AllowAnonymous, HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot(ForgotPasswordRequest request, CancellationToken ct)
    {
        await useCase.ForgotAsync(request.Email, ct);
        return Ok(new { Message = "If an eligible account exists, a password reset email has been sent." });
    }
    [AllowAnonymous, HttpPost("reset-password")]
    public async Task<IActionResult> Reset(ResetPasswordRequest request, CancellationToken ct) => Reply(await useCase.ResetAsync(request.AccountId, request.Token, request.NewPassword, ct));
    [Authorize, HttpPost("change-password")]
    public async Task<IActionResult> Change(ChangePasswordRequest request, CancellationToken ct)
    {
        if (!int.TryParse(User.FindFirst("sub")?.Value, out var id) || id <= 0) return Unauthorized();
        return Reply(await useCase.ChangeAsync(id, request.OldPassword, request.NewPassword, ct));
    }
    private IActionResult Reply(Result<bool> result) => result.IsSuccess ? NoContent() : StatusCode(result.Error!.Type switch
    { ErrorType.Forbidden => 403, ErrorType.Conflict => 409, _ => 400 }, new { result.Error.Code, result.Error.Description });
}
