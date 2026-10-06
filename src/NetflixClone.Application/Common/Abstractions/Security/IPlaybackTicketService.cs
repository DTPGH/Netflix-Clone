namespace NetflixClone.Application.Common.Abstractions.Security;
public sealed record PlaybackTicket(int UserAccountId, int ProfileId, int MovieId, string MediaKey, DateTime ExpiresAtUtc, string? ProfileUnlockToken = null);
public sealed record IssuedPlaybackTicket(string Token, DateTime ExpiresAtUtc);
public interface IPlaybackTicketService
{
    IssuedPlaybackTicket Issue(int accountId, int profileId, int movieId, string mediaKey, string? profileUnlockToken = null);
    PlaybackTicket? Validate(string token);
}
