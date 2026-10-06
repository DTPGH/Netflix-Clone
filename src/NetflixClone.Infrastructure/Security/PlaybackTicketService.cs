using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using NetflixClone.Application.Common.Abstractions.Security;
using NetflixClone.Application.Common.Abstractions.Time;
namespace NetflixClone.Infrastructure.Security;
public sealed class PlaybackTicketService(IDataProtectionProvider provider, IClock clock) : IPlaybackTicketService
{
    private readonly IDataProtector protector = provider.CreateProtector("NetflixClone.PlaybackTicket.v1");
    public IssuedPlaybackTicket Issue(int accountId, int profileId, int movieId, string mediaKey, string? profileUnlockToken = null)
    {
        var expires = clock.UtcNow.AddMinutes(5);
        var data = new PlaybackTicket(accountId, profileId, movieId, mediaKey, expires, profileUnlockToken);
        return new(protector.Protect(JsonSerializer.Serialize(data)), expires);
    }
    public PlaybackTicket? Validate(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 4096) return null;
        try
        {
            var data = JsonSerializer.Deserialize<PlaybackTicket>(protector.Unprotect(token));
            return data is { UserAccountId: > 0, ProfileId: > 0, MovieId: > 0 } &&
                data.ExpiresAtUtc > clock.UtcNow && data.ExpiresAtUtc <= clock.UtcNow.AddMinutes(5) &&
                !string.IsNullOrEmpty(data.MediaKey) ? data : null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or FormatException or ArgumentException) { return null; }
    }
}
