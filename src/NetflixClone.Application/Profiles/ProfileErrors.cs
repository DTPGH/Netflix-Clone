using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Profiles;
public static class ProfileErrors
{
    public static readonly Error InvalidPin = new("Profiles.InvalidPin", "PIN must contain exactly four digits (0–9).", ErrorType.Validation);
    public static readonly Error WrongPin = new("Profiles.WrongPin", "The PIN is incorrect.", ErrorType.Validation);
    public static readonly Error PinTemporarilyLocked = new("Profiles.PinTemporarilyLocked", "Too many incorrect PIN attempts. Try again in five minutes.", ErrorType.Forbidden);
    public static readonly Error UnlockRequired = new("Profiles.UnlockRequired", "Unlock this profile before continuing.", ErrorType.Forbidden);
    public static readonly Error WrongAccountPassword = new("Profiles.WrongAccountPassword", "The account password is incorrect.", ErrorType.Validation);
    public static readonly Error InvalidName = new("Profiles.InvalidName", "Name must contain between 1 and 100 characters after trimming.", ErrorType.Validation);
    public static readonly Error NotFound = new("Profiles.NotFound", "The profile was not found.", ErrorType.NotFound);
    public static readonly Error AccountNotFound = new("Profiles.AccountNotFound", "The authenticated account is unavailable.", ErrorType.Unauthorized);
    public static readonly Error LimitReached = new("Profiles.LimitReached", "An account may have at most 5 active profiles.", ErrorType.Conflict);
    public static readonly Error ConcurrentChange = new("Profiles.ConcurrentChange", "The profile changed. Reload and try again.", ErrorType.Conflict);
}
