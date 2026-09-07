namespace NetflixClone.Application.Common.Abstractions.Messaging;

public interface IEmailConfirmationSender
{
    Task SendAsync(
        int userAccountId,
        string email,
        string rawToken,
        CancellationToken cancellationToken = default);
}