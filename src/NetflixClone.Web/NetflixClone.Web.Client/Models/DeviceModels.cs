namespace NetflixClone.Web.Client.Models;

public sealed record DeviceReply(int DeviceId, string DeviceName, string DeviceType,
    DateTimeOffset FirstSeenAtUtc, DateTimeOffset LastActiveAtUtc, DateTimeOffset? RevokedAtUtc);
public sealed record DevicesReply(List<DeviceReply> Devices);

