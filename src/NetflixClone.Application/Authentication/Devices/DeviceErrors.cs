using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Authentication.Devices;
public static class DeviceErrors
{
    public static readonly Error NotFound = new("Auth.Devices.NotFound", "The device was not found.", ErrorType.NotFound);
    public static readonly Error ConcurrentChange = new("Auth.Devices.ConcurrentChange",
        "The device or its sessions changed. Please try again.", ErrorType.Conflict);
}
