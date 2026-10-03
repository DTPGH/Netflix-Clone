namespace NetflixClone.Application.Subscriptions;
public static class SubscriptionRules
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);
    public const string PaymentMethod = "Simulated";
}
public sealed record AvailablePlan(int PlanId, string Name, decimal Price, string Currency, int DurationDays,
    int MaxConcurrentStreams, string MaxQuality, DateTime UpdatedAtUtc);
public sealed record SubscriptionInfo(int SubscriptionId, int PlanId, string PlanName, DateTime StartDateUtc,
    DateTime? EndDateUtc, string Status, bool IsEffective, bool AutoRenew);
public sealed record SubscriptionOverview(SubscriptionInfo? Current, SubscriptionInfo? Latest);
public sealed record PaymentInfo(int PaymentId, int SubscriptionId, int PlanId, string PlanName, decimal Amount,
    string Currency, string Method, string TransactionCode, string Status, DateTime? PaidAtUtc, DateTime CreatedAtUtc);
public sealed record PaymentPage(IReadOnlyList<PaymentInfo> Items, int TotalCount, int Page, int PageSize);
public sealed record PurchaseSubscription(int AccountId, int PlanId, Guid IdempotencyKey, DateTime ExpectedPlanUpdatedAtUtc);
public sealed record PurchaseResult(SubscriptionInfo Subscription, PaymentInfo Payment, bool Replayed);
