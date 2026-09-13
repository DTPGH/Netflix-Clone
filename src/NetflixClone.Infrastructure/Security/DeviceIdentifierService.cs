using System.Security.Cryptography;
using System.Text;
using NetflixClone.Application.Common.Abstractions.Security;

namespace NetflixClone.Infrastructure.Security;

public sealed class DeviceIdentifierService : IDeviceIdentifierService
{
    public GeneratedDeviceIdentifier Generate()
    {
        var identifier = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        return new GeneratedDeviceIdentifier(identifier, HashIfValid(identifier)!);
    }

    public string? HashIfValid(string? identifier)
    {
        if (identifier is null || identifier.Length != 64 || !identifier.All(Uri.IsHexDigit))
        {
            return null;
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identifier.ToUpperInvariant())));
    }
}
