using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Profiles;

namespace NetflixClone.Infrastructure.Security;

public sealed class ProfileUnlockTokenService(IDataProtectionProvider provider, IClock clock) : IProfileUnlockTokenService
{
    private readonly IDataProtector protector = provider.CreateProtector("NetflixClone.ProfileUnlock.v1");
    private sealed record Payload(int AccountId, int ProfileId, string Stamp, DateTime ExpiresAtUtc, Guid Nonce);
    // Binding to both salted hashes invalidates old grants after PIN or account-password changes.
    private static byte[] Stamp(string? pinHash, string passwordHash)
        => SHA256.HashData(Encoding.UTF8.GetBytes((pinHash ?? "") + ":" + passwordHash));

    public GeneratedProfileUnlockToken Generate(int accountId, int profileId, string? pinHash, string accountPasswordHash)
    {
        var expires = clock.UtcNow.Add(ProfilePinRules.UnlockLifetime);
        var payload = new Payload(accountId, profileId, Convert.ToHexString(Stamp(pinHash, accountPasswordHash)), expires, Guid.NewGuid());
        return new(protector.Protect(JsonSerializer.Serialize(payload)), expires);
    }

    public bool Verify(string token, int accountId, int profileId, string? pinHash, string accountPasswordHash)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 2048) return false;
        try
        {
            var data = JsonSerializer.Deserialize<Payload>(protector.Unprotect(token));
            var now = clock.UtcNow;
            return data is not null && data.AccountId == accountId && data.ProfileId == profileId &&
                data.ExpiresAtUtc > now && data.ExpiresAtUtc <= now.Add(ProfilePinRules.UnlockLifetime) &&
                CryptographicOperations.FixedTimeEquals(Convert.FromHexString(data.Stamp), Stamp(pinHash, accountPasswordHash));
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException or ArgumentException)
        { return false; }
    }
}
