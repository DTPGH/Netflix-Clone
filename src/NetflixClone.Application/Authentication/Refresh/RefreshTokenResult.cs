namespace NetflixClone.Application.Authentication.Refresh;
public sealed record RefreshTokenResult(string AccessToken, DateTime ExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);
