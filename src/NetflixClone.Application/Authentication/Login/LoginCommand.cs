namespace NetflixClone.Application.Authentication.Login;

public sealed record LoginCommand(
    string Email, string Password, string? DeviceIdentifier,
    string DeviceName, string DeviceType, string? UserAgent, string? RemoteIpAddress);
