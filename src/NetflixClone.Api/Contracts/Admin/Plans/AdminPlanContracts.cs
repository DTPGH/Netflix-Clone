using NetflixClone.Application.Admin.Plans;
namespace NetflixClone.Api.Contracts.Admin.Plans;
public sealed record PlanResponse(int PlanId, string Name, decimal Price, int MaxConcurrentStreams, string MaxQuality,
    bool IsActive, int SubscriptionCount, DateTime CreatedAtUtc, DateTime UpdatedAtUtc)
{
    public static PlanResponse From(AdminPlan p) => new(p.PlanId, p.Name, p.Price, p.MaxConcurrentStreams, p.MaxQuality,
        p.IsActive, p.SubscriptionCount, p.CreatedAtUtc, p.UpdatedAtUtc);
}
public sealed record SavePlanRequest(string? Name, decimal Price, int MaxConcurrentStreams, string? MaxQuality, DateTime? ExpectedUpdatedAtUtc);
public sealed record PlanStatusRequest(bool IsActive, DateTime? ExpectedUpdatedAtUtc);
