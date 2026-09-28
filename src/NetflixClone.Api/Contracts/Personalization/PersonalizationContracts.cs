using NetflixClone.Api.Contracts.Catalog;
namespace NetflixClone.Api.Contracts.Personalization;
public sealed record CompleteOnboardingRequest(int[]? MovieIds);
public sealed record OnboardingResponse(bool OnboardingCompleted, IReadOnlyList<int> MovieIds);
public sealed record RecommendationsResponse(IReadOnlyList<MovieSummaryResponse> Items, string Source);
public sealed class OnboardingMoviesRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
}
