namespace NetflixClone.Web.Client.Models;
public sealed record SubscriptionPlan(int PlanId, string Name, decimal Price, string Currency, int DurationDays,
    int MaxConcurrentStreams, string MaxQuality, DateTime UpdatedAtUtc);
public sealed record SubscriptionInfo(int SubscriptionId, int PlanId, string PlanName, DateTime StartDateUtc,
    DateTime? EndDateUtc, string Status, bool IsEffective, bool AutoRenew);
public sealed record SubscriptionOverview(SubscriptionInfo? Current, SubscriptionInfo? Latest);
public sealed record SubscriptionPayment(int PaymentId, int SubscriptionId, int PlanId, string PlanName, decimal Amount,
    string Currency, string Method, string TransactionCode, string Status, DateTime? PaidAtUtc, DateTime CreatedAtUtc);
public sealed record SubscriptionPaymentPage(SubscriptionPayment[] Items, int TotalCount, int Page, int PageSize);
public sealed record SubscriptionPurchaseResult(SubscriptionInfo Subscription, SubscriptionPayment Payment, bool Replayed);
public sealed record SubscriptionPurchaseDraft(int PlanId, string PlanName, decimal DisplayPrice, Guid IdempotencyKey,
    DateTime ExpectedPlanUpdatedAtUtc, bool Submitted);
