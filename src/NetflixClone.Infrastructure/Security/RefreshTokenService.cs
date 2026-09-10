using System.Security.Cryptography;
using System.Text;
using NetflixClone.Application.Common.Abstractions.Security;

namespace NetflixClone.Infrastructure.Security;

public sealed class RefreshTokenService : IRefreshTokenService
{
    public GeneratedRefreshToken Generate()
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
        return new GeneratedRefreshToken(rawToken, hash);
    }
}
