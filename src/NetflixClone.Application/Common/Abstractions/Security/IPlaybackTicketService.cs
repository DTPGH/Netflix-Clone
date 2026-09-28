namespace NetflixClone.Application.Common.Abstractions.Security;
public sealed record PlaybackTicket(int UserAccountId, int ProfileId, int MovieId, string MediaKey, DateTime ExpiresAtUtc);
public sealed record IssuedPlaybackTicket(string Token, DateTime ExpiresAtUtc);
public interface IPlaybackTicketService
{
    IssuedPlaybackTicket Issue(int accountId, int profileId, int movieId, string mediaKey);
    PlaybackTicket? Validate(string token);
}
