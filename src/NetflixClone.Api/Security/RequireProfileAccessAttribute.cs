using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NetflixClone.Application.Common.Results;
using NetflixClone.Application.Profiles;

namespace NetflixClone.Api.Security;

// Applied to every profile-content controller. [Authorize] still validates the account JWT first.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireProfileAccessAttribute() : TypeFilterAttribute(typeof(ProfileAccessFilter));

public sealed class ProfileAccessFilter(IProfileAccessGuard guard) : IAsyncActionFilter
{
    public const string HeaderName = "X-Profile-Unlock";
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!int.TryParse(context.HttpContext.User.FindFirst("sub")?.Value, out var accountId) || accountId <= 0)
        { context.Result = new UnauthorizedResult(); return; }
        if (!int.TryParse(context.RouteData.Values["profileId"]?.ToString(), out var profileId) || profileId <= 0)
        { context.Result = new BadRequestResult(); return; }
        var headers = context.HttpContext.Request.Headers[HeaderName];
        var token = headers.Count == 1 ? headers[0] : null;
        var result = await guard.CheckAsync(accountId, profileId, token, context.HttpContext.RequestAborted);
        if (result.IsFailure)
        {
            context.Result = new ObjectResult(new { result.Error!.Code, result.Error.Description })
            { StatusCode = result.Error.Type switch { ErrorType.NotFound => 404, ErrorType.Unauthorized => 401, _ => 403 } };
            return;
        }
        await next();
    }
}
