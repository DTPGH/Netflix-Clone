using Microsoft.Extensions.Logging;
using NetflixClone.Application.Common.Abstractions.Messaging;

namespace NetflixClone.Infrastructure.Messaging;

public sealed class DevelopmentEmailConfirmationSender
    : IEmailConfirmationSender
{
    private readonly ILogger<DevelopmentEmailConfirmationSender> _logger;

    public DevelopmentEmailConfirmationSender(ILogger<DevelopmentEmailConfirmationSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(int userAccountId, string email, string rawToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            """
            EMAIL CONFIRMATION (DEVELOPMENT ONLY)
            AccountId: {UserAccountId}
            Email: {Email}
            Token: {Token}
            """,
            userAccountId,
            email,
            rawToken);

        return Task.CompletedTask;
    }
}