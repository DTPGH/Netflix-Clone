namespace NetflixClone.Application.Ratings;
public sealed record RatingQuery(int UserAccountId, int ProfileId, int MovieId);
public sealed record SetRatingCommand(int UserAccountId, int ProfileId, int MovieId, string? Value);
public sealed record RatingResult(int MovieId, string? Value);
public static class RatingRules
{
    public static bool IsValid(string? value) => value is "NotForMe" or "Like" or "Love";
    public static DateTime NextUpdatedAt(DateTime previous, DateTime now)
        => now > previous ? now : previous.AddTicks(1);
}
