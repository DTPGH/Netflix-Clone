using NetflixClone.Application.Catalog.Movies;
namespace NetflixClone.Application.Personalization;
public sealed record ProfileContext(int UserAccountId, int ProfileId);
public sealed record OnboardingState(bool OnboardingCompleted, IReadOnlyList<int> MovieIds);
public sealed record CompleteOnboardingCommand(int UserAccountId, int ProfileId, IReadOnlyList<int>? MovieIds);
public sealed record OnboardingMoviesQuery(int UserAccountId, int ProfileId, int Page = 1, int PageSize = 20, string? Search = null);
public sealed record RecommendationsQuery(int UserAccountId, int ProfileId, int Limit = 12);
public sealed record RecommendationsResult(IReadOnlyList<MovieSummary> Items, string Source);
public static class PersonalizationRules
{
    public const int MaxSelections = 5;
    public const int PreferenceWeight = 1;
    public const int LikeWeight = 2;
    public const int LoveWeight = 3;
    public const int MaxRecommendationLimit = 24;
}
