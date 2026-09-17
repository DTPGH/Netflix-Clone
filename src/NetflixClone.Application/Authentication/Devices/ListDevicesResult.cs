namespace NetflixClone.Application.Authentication.Devices;
public sealed record ListDevicesResult(IReadOnlyList<DeviceSummary> Devices);
