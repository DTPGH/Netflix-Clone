namespace NetflixClone.Application.Common.Abstractions.Security;

public sealed record GeneratedEmailConfirmationToken(string RawToken, string TokenHash);

public interface IEmailConfirmationTokenService
{
    GeneratedEmailConfirmationToken Generate();

    bool Verify(string rawToken, string tokenHash);
}