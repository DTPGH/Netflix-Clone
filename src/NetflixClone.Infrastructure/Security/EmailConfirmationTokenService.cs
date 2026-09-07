using System.Security.Cryptography;
using System.Text;
using NetflixClone.Application.Common.Abstractions.Security;

namespace NetflixClone.Infrastructure.Security;

public sealed class EmailConfirmationTokenService : IEmailConfirmationTokenService
{
    public GeneratedEmailConfirmationToken Generate()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);

        var rawToken = Convert.ToHexString(tokenBytes);

        var tokenHash = ComputeHash(rawToken);

        return new GeneratedEmailConfirmationToken(rawToken, tokenHash);
    }

    public bool Verify(string rawToken, string tokenHash)
    {
        var computedHash = ComputeHash(rawToken);

        var expectedBytes = Convert.FromHexString(tokenHash);

        var actualBytes = Convert.FromHexString(computedHash);

        return CryptographicOperations.FixedTimeEquals(
            expectedBytes,
            actualBytes);
    }

    private static string ComputeHash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);

        var hashBytes = SHA256.HashData(bytes);

        return Convert.ToHexString(hashBytes);
    }
}