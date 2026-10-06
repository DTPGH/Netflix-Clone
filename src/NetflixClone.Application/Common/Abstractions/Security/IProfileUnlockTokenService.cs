namespace NetflixClone.Application.Common.Abstractions.Security;

public sealed record GeneratedProfileUnlockToken(string Token, DateTime ExpiresAtUtc);
public interface IProfileUnlockTokenService
{
    GeneratedProfileUnlockToken Generate(int accountId, int profileId, string? pinHash, string accountPasswordHash);
    bool Verify(string token, int accountId, int profileId, string? pinHash, string accountPasswordHash);
}
