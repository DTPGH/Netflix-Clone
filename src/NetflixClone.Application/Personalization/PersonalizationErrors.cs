using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Personalization;
public static class PersonalizationErrors
{
    public static readonly Error ProfileNotFound = new("Personalization.ProfileNotFound", "The profile was not found.", ErrorType.NotFound);
    public static readonly Error InvalidSelection = new("Onboarding.InvalidSelection", "Supply up to five distinct positive movie IDs. An empty list skips onboarding.", ErrorType.Validation);
    public static readonly Error InvalidMovies = new("Onboarding.InvalidMovies", "Some selected movies are no longer eligible. Reload and select again.", ErrorType.Validation);
    public static readonly Error InvalidQuery = new("Personalization.InvalidQuery", "The search, page or limit is invalid.", ErrorType.Validation);
    public static readonly Error ConcurrentChange = new("Onboarding.ConcurrentChange", "The profile or onboarding changed. Reload before continuing.", ErrorType.Conflict);
    public static readonly Error AlreadyCompleted = new("Onboarding.AlreadyCompleted", "Onboarding has already been completed with a different selection.", ErrorType.Conflict);
}
