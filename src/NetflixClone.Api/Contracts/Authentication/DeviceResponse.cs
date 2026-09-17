namespace NetflixClone.Api.Contracts.Authentication;
public sealed record DeviceResponse(int DeviceId, string DeviceName, string DeviceType,
    DateTime FirstSeenAtUtc, DateTime LastActiveAtUtc, DateTime? RevokedAtUtc);
