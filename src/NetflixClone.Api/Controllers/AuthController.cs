// using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NetflixClone.Api.Contracts.Authentication;
using NetflixClone.Application.Authentication.EmailConfirmation;
using NetflixClone.Application.Authentication.EmailConfirmation.Resend;
using NetflixClone.Application.Authentication.Register;
using NetflixClone.Application.Authentication.Login;
using NetflixClone.Application.Authentication.Refresh;
using NetflixClone.Application.Common.Results;

namespace NetflixClone.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IRegisterAccountUseCase _registerAccountUseCase;
    private readonly IConfirmEmailUseCase _confirmEmailUseCase;
    private readonly IResendEmailConfirmationUseCase _resendEmailConfirmationUseCase;
    private readonly ILoginUseCase _loginUseCase;
    private readonly IRefreshTokenUseCase _refreshTokenUseCase;

    public AuthController(IRegisterAccountUseCase registerAccountUseCase, IConfirmEmailUseCase confirmEmailUseCase, IResendEmailConfirmationUseCase resendEmailConfirmationUseCase, ILoginUseCase loginUseCase, IRefreshTokenUseCase refreshTokenUseCase)
    {
        _registerAccountUseCase = registerAccountUseCase;
        _confirmEmailUseCase = confirmEmailUseCase;
        _resendEmailConfirmationUseCase = resendEmailConfirmationUseCase;
        _loginUseCase = loginUseCase;
        _refreshTokenUseCase = refreshTokenUseCase;
    }

    [AllowAnonymous]
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await _refreshTokenUseCase.ExecuteAsync(
            new RefreshTokenCommand(request.RefreshToken), cancellationToken);
        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(new { result.Error.Code, result.Error.Description }),
                _ => Problem(statusCode: StatusCodes.Status500InternalServerError,
                    title: "An unexpected error occurred.")
            };
        }

        return Ok(new RefreshTokenResponse(
            result.Value!.AccessToken, result.Value.ExpiresAtUtc,
            result.Value.RefreshToken, result.Value.RefreshTokenExpiresAtUtc));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var userAgent = Request.Headers.UserAgent.ToString();
        var command = new LoginCommand(
            request.Email, request.Password, request.DeviceIdentifier,
            request.DeviceName, request.DeviceType,
            userAgent.Length > 500 ? userAgent[..500] : userAgent,
            HttpContext.Connection.RemoteIpAddress?.ToString());
        var result = await _loginUseCase.ExecuteAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
                ErrorType.Unauthorized => Unauthorized(new
                {
                    result.Error.Code,
                    result.Error.Description
                }),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new
                {
                    result.Error.Code,
                    result.Error.Description
                }),
                _ => Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "An unexpected error occurred.")
            };
        }

        return Ok(new LoginResponse(
            result.Value!.UserAccountId, result.Value.Email,
            result.Value.AccessToken, result.Value.ExpiresAtUtc,
            result.Value.RefreshToken, result.Value.RefreshTokenExpiresAtUtc,
            result.Value.DeviceIdentifier));
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            UserAccountId = User.FindFirst("sub")!.Value,
            Roles = User.FindAll("role").Select(claim => claim.Value).ToArray()
        });
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

    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var command = new ConfirmEmailCommand(request.UserAccountId, request.Token);

        var result = await _confirmEmailUseCase.ExecuteAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error!.Type switch
            {
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

        return Ok(new
        {
            result.Value!.UserAccountId,
            result.Value.Email,
            result.Value.EmailConfirmed
        });
    }

    [HttpPost("resend-email-confirmation")]
    public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationRequest request, CancellationToken cancellationToken)
    {
        var command = new ResendEmailConfirmationCommand(request.Email);

        await _resendEmailConfirmationUseCase.ExecuteAsync(command, cancellationToken);

        return Ok(new
        {
            message =
                "If an unconfirmed account exists for this email, " +
                "a confirmation email has been sent."
        });
    }
}
