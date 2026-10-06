namespace NetflixClone.Application.Profiles;

public static class ProfilePinRules
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan UnlockLifetime = TimeSpan.FromMinutes(30);
    public static bool IsValid(string? pin) => pin is { Length: 4 } && pin.All(c => c is >= '0' and <= '9');
}
