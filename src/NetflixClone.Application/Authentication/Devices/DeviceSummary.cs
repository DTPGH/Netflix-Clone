namespace NetflixClone.Application.Authentication.Devices;
public sealed record DeviceSummary(int DeviceId, string DeviceName, string DeviceType,
    DateTime FirstSeenAtUtc, DateTime LastActiveAtUtc, DateTime? RevokedAtUtc);
