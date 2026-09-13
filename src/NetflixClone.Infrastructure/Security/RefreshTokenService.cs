using System.Security.Cryptography;
using System.Text;
using NetflixClone.Application.Common.Abstractions.Security;

namespace NetflixClone.Infrastructure.Security;

public sealed class RefreshTokenService : IRefreshTokenService
{
    public string? HashIfValid(string? rawToken)
    {
        if (rawToken is null || rawToken.Length != 64 || !rawToken.All(Uri.IsHexDigit))
        {
            return null;
        }

        // Hash the exact text issued by Generate; do not trim or change its case.
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }

    public GeneratedRefreshToken Generate()
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        return new GeneratedRefreshToken(rawToken, hash);
    }
}
