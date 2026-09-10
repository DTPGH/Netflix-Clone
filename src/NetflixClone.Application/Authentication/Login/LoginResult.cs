namespace NetflixClone.Application.Authentication.Login;

public sealed record LoginResult(
    int UserAccountId, string Email, string AccessToken, DateTime ExpiresAtUtc,
    string RefreshToken, DateTime RefreshTokenExpiresAtUtc, string DeviceIdentifier);
