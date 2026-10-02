namespace NetflixClone.Application.Admin.Plans;
public sealed record AdminPlan(int PlanId, string Name, decimal Price, int MaxConcurrentStreams, string MaxQuality,
    bool IsActive, int SubscriptionCount, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record SaveAdminPlan(int ActorId, int? PlanId, string? Name, decimal Price, int MaxConcurrentStreams,
    string? MaxQuality, DateTime? ExpectedUpdatedAtUtc);
