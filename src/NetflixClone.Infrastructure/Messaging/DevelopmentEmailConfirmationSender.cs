using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetflixClone.Application.Common.Abstractions.Messaging;

namespace NetflixClone.Infrastructure.Messaging;

public sealed class DevelopmentEmailConfirmationSender
    : IEmailConfirmationSender
{
    private readonly ILogger<DevelopmentEmailConfirmationSender> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IConfiguration _config;

    public DevelopmentEmailConfirmationSender(ILogger<DevelopmentEmailConfirmationSender> logger, IHostEnvironment environment, IConfiguration config)
    {
        _logger = logger;
        _environment = environment;
        _config = config;
    }

    public Task SendAsync(int userAccountId, string email, string rawToken, CancellationToken cancellationToken = default)
    {
        if (!_environment.IsDevelopment())
            throw new InvalidOperationException("Configure a real email confirmation sender before using this feature outside development.");
        if (!Uri.TryCreate(_config["EmailConfirmation:WebOrigin"], UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !uri.IsLoopback || uri.AbsolutePath != "/" || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new InvalidOperationException("EmailConfirmation:WebOrigin must be the HTTPS localhost Web origin.");
        _logger.LogInformation("EMAIL CONFIRMATION (LOCAL DEVELOPMENT ONLY) for {Email}: {Link}", email,
            $"{uri.GetLeftPart(UriPartial.Authority)}/confirm-email#accountId={userAccountId}&token={Uri.EscapeDataString(rawToken)}");

        return Task.CompletedTask;
    }
}
