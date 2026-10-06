using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetflixClone.Application.Authentication.Passwords;
namespace NetflixClone.Infrastructure.Messaging;
public sealed class DevelopmentPasswordResetSender(IHostEnvironment environment, IConfiguration config, ILogger<DevelopmentPasswordResetSender> logger) : IPasswordResetSender
{
    public Task SendAsync(int accountId, string email, string token, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Configure a real password reset email sender before using this feature outside development.");
        var origin = config["PasswordReset:WebOrigin"];
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != "https" || !uri.IsLoopback || uri.AbsolutePath != "/" || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidOperationException("PasswordReset:WebOrigin must be the HTTPS localhost Web origin.");
        logger.LogInformation("PASSWORD RESET (LOCAL DEVELOPMENT ONLY) for {Email}: {Link}", email,
            $"{uri.GetLeftPart(UriPartial.Authority)}/reset-password#accountId={accountId}&token={Uri.EscapeDataString(token)}");
        return Task.CompletedTask;
    }
}
