namespace NetflixClone.Api.Contracts.Subscriptions;
public sealed record PurchaseSubscriptionRequest(int PlanId, Guid IdempotencyKey, DateTime ExpectedPlanUpdatedAtUtc);
