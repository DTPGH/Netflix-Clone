namespace NetflixClone.Api.Contracts.Authentication;
public sealed record RefreshTokenResponse(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);
