namespace NetflixClone.Application.Authentication.Devices;
public sealed record RevokeDeviceCommand(int UserAccountId, int DeviceId);
