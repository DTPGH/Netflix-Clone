namespace NetflixClone.Application.Profiles;
public static class ProfileRules
{
    public const int MaxActiveProfiles = 5;
    public const int MaxNameLength = 100;
    public static byte MaturityLevel(bool isKids) => (byte)(isKids ? 13 : 18);
    public static string? NormalizeName(string? name)
    {
        var normalized = name?.Trim();
        return string.IsNullOrEmpty(normalized) || normalized.Length > MaxNameLength ? null : normalized;
    }
    // datetime2 preserves ticks. Always change the optimistic concurrency value.
    public static DateTime NextUpdatedAt(DateTime previous, DateTime utcNow)
        => utcNow > previous ? utcNow : previous.AddTicks(1);
}
