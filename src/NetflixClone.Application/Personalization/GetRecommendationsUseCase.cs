using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Results;
namespace NetflixClone.Application.Personalization;
public interface IGetRecommendationsUseCase
{
    Task<Result<RecommendationsResult>> ExecuteAsync(RecommendationsQuery query, CancellationToken ct = default);
}
public sealed class GetRecommendationsUseCase(IProfileRepository profiles, IPersonalizationQueries movies) : IGetRecommendationsUseCase
{
    public async Task<Result<RecommendationsResult>> ExecuteAsync(RecommendationsQuery query, CancellationToken ct = default)
    {
        var profile = await profiles.GetByIdForAccountAsync(query.UserAccountId, query.ProfileId, ct);
        if (profile is null || profile.IsDeleted) return Result<RecommendationsResult>.Failure(PersonalizationErrors.ProfileNotFound);
        if (query.Limit is < 1 or > PersonalizationRules.MaxRecommendationLimit)
            return Result<RecommendationsResult>.Failure(PersonalizationErrors.InvalidQuery);
        return Result<RecommendationsResult>.Success(await movies.GetRecommendationsAsync(profile.Id, profile.MaturityLevel, query.Limit, ct));
    }
}
