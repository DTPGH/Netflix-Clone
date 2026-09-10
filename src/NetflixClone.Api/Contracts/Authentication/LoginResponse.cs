namespace NetflixClone.Api.Contracts.Authentication;

public sealed record LoginResponse(
    int UserAccountId, string Email, string AccessToken, DateTime ExpiresAtUtc,
    string RefreshToken, DateTime RefreshTokenExpiresAtUtc, string DeviceIdentifier);
