// using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using NetflixClone.Api.Contracts.Authentication;
using NetflixClone.Application.Authentication.Register;
using NetflixClone.Application.Common.Results;

namespace NetflixClone.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IRegisterAccountUseCase _registerAccountUseCase;
    public AuthController(IRegisterAccountUseCase registerAccountUseCase)
    {
        _registerAccountUseCase = registerAccountUseCase;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterAccountCommand(request.Email, request.Password);
        var result = await _registerAccountUseCase.ExecuteAsync(command, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Conflict =>
                    Conflict(new
                    {
                        result.Error.Code,
                        result.Error.Description
                    }),

                ErrorType.Validation =>
                    BadRequest(new
                    {
                        result.Error.Code,
                        result.Error.Description
                    }),

                _ => Problem(
                    statusCode:
                        StatusCodes.Status500InternalServerError,
                    title: "An unexpected error occurred.")
            };
        }
        var response = new RegisterResponse(result.Value!.UserAccountId, result.Value.Email, result.Value.EmailConfirmed);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}