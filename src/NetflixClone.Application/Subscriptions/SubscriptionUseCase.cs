using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Subscriptions;
public interface ISubscriptionUseCase
{
    Task<Result<IReadOnlyList<AvailablePlan>>> PlansAsync(int accountId, CancellationToken ct);
    Task<Result<SubscriptionOverview>> CurrentAsync(int accountId, CancellationToken ct);
    Task<Result<PaymentPage>> PaymentsAsync(int accountId, int page, CancellationToken ct);
    Task<Result<PurchaseResult>> PurchaseAsync(PurchaseSubscription command, CancellationToken ct);
}
public sealed class SubscriptionUseCase(ISubscriptionRepository subscriptions, ISubscriptionPurchaseScopeFactory scopes,
    IUserAccountRepository accounts, IUnitOfWork unitOfWork, IClock clock) : ISubscriptionUseCase
{
    private static readonly Error Forbidden = new("Subscriptions.Forbidden", "An unlocked, email-confirmed account is required.", ErrorType.Forbidden);
    private static readonly Error Integrity = new("Subscriptions.InvalidState", "Subscription data needs administrator attention.", ErrorType.Failure);
    private async Task<bool> Allowed(int id, CancellationToken ct)
    {
        var account = await accounts.GetByIdAsync(id, ct);
        return account is not null && !account.IsLocked && account.EmailConfirmed;
    }
    private static DateTime Utc(DateTime date) => DateTime.SpecifyKind(date, DateTimeKind.Utc);
    private static bool Effective(Subscription s, DateTime now) => s.Status == "Active" && s.StartDate <= now && s.EndDate > now;
    private static bool InvalidRows(IReadOnlyList<Subscription> rows, DateTime now) =>
        rows.Any(s => s.EndDate is null || s.StartDate > now) || rows.Count(s => Effective(s, now)) > 1;
    private static SubscriptionInfo Map(Subscription s, DateTime now) => new(s.Id, s.PlanId, s.Plan.Name, Utc(s.StartDate),
        s.EndDate is { } end ? Utc(end) : null, s.Status == "Active" && s.EndDate <= now ? "Expired" : s.Status, Effective(s, now), s.AutoRenew);
    private static PaymentInfo Map(PaymentTransaction p) => new(p.Id, p.SubscriptionId, p.Subscription.PlanId, p.Subscription.Plan.Name,
        p.Amount, "VND", p.Method, p.TransactionCode, p.Status, p.PaidAt is { } date ? Utc(date) : null, Utc(p.CreatedAt));
    public async Task<Result<IReadOnlyList<AvailablePlan>>> PlansAsync(int accountId, CancellationToken ct) =>
        !await Allowed(accountId, ct) ? Result<IReadOnlyList<AvailablePlan>>.Failure(Forbidden)
            : Result<IReadOnlyList<AvailablePlan>>.Success(await subscriptions.ListPlansAsync(ct));
    public async Task<Result<SubscriptionOverview>> CurrentAsync(int accountId, CancellationToken ct)
    {
        if (!await Allowed(accountId, ct)) return Result<SubscriptionOverview>.Failure(Forbidden);
        var rows = await subscriptions.GetActiveRowsAsync(accountId, ct); var now = clock.UtcNow;
        if (InvalidRows(rows, now)) return Result<SubscriptionOverview>.Failure(Integrity);
        var current = rows.SingleOrDefault(s => Effective(s, now));
        var latest = current ?? await subscriptions.GetLatestAsync(accountId, ct);
        return Result<SubscriptionOverview>.Success(new(current is null ? null : Map(current, now), latest is null ? null : Map(latest, now)));
    }
    public async Task<Result<PaymentPage>> PaymentsAsync(int accountId, int page, CancellationToken ct)
    {
        if (!await Allowed(accountId, ct)) return Result<PaymentPage>.Failure(Forbidden);
        if (page is < 1 or > 1000000) return Result<PaymentPage>.Failure(new("Subscriptions.InvalidPage", "Invalid history page.", ErrorType.Validation));
        return Result<PaymentPage>.Success(await subscriptions.ListPaymentsAsync(accountId, page, ct));
    }
    public async Task<Result<PurchaseResult>> PurchaseAsync(PurchaseSubscription command, CancellationToken ct)
    {
        if (command.PlanId <= 0 || command.IdempotencyKey == Guid.Empty || command.ExpectedPlanUpdatedAtUtc.Kind != DateTimeKind.Utc)
            return Result<PurchaseResult>.Failure(new("Subscriptions.InvalidPurchase", "Choose a plan and provide a valid request key and UTC plan version.", ErrorType.Validation));
        await using var scope = await scopes.BeginAsync(command.AccountId, ct);
        if (scope is null || !await Allowed(command.AccountId, ct)) return Result<PurchaseResult>.Failure(Forbidden);
        var code = $"SIM-{command.AccountId}-{command.IdempotencyKey:N}";
        var existing = await subscriptions.FindPaymentAsync(command.AccountId, code, ct);
        var now = clock.UtcNow;
        if (existing is not null)
        {
            if (existing.Subscription.PlanId != command.PlanId)
                return Result<PurchaseResult>.Failure(new("Subscriptions.IdempotencyConflict", "This request key was used for another plan.", ErrorType.Conflict));
            if (existing.Status != "Succeeded" || existing.Method != SubscriptionRules.PaymentMethod) return Result<PurchaseResult>.Failure(Integrity);
            // An already committed attempt is returned even after plan changes or subscription expiry.
            return Result<PurchaseResult>.Success(new(Map(existing.Subscription, now), Map(existing), true));
        }
        var activeRows = await subscriptions.GetActiveRowsAsync(command.AccountId, ct);
        if (InvalidRows(activeRows, now)) return Result<PurchaseResult>.Failure(Integrity);
        if (activeRows.Any(s => Effective(s, now)))
            return Result<PurchaseResult>.Failure(new("Subscriptions.SubscriptionAlreadyActive", "Your current plan is still active. Purchase again after it expires.", ErrorType.Conflict));
        var plan = await subscriptions.GetPlanForPurchaseAsync(command.PlanId, ct);
        if (plan is null || !plan.IsActive)
            return Result<PurchaseResult>.Failure(new("Subscriptions.PlanUnavailable", "This plan is no longer offered.", ErrorType.Conflict));
        if (plan.UpdatedAt.Ticks != command.ExpectedPlanUpdatedAtUtc.Ticks)
            return Result<PurchaseResult>.Failure(new("Subscriptions.PlanChanged", "This plan changed. Reload and confirm its current price.", ErrorType.Conflict));
        // Sample after waits/plan locks so all successful timestamps share one clock value.
        now = clock.UtcNow;
        foreach (var old in activeRows.Where(s => s.EndDate <= now))
        { old.Status = "Expired"; old.UpdatedAt = now > old.UpdatedAt ? now : old.UpdatedAt.AddTicks(1); }
        var subscription = new Subscription { UserAccountId = command.AccountId, PlanId = plan.Id, Plan = plan,
            StartDate = now, EndDate = now + SubscriptionRules.Lifetime, Status = "Active", AutoRenew = false, CreatedAt = now, UpdatedAt = now };
        var payment = new PaymentTransaction { Subscription = subscription, Amount = plan.Price,
            Method = SubscriptionRules.PaymentMethod, TransactionCode = code, Status = "Succeeded", PaidAt = now, CreatedAt = now };
        subscriptions.Add(subscription, payment);
        try { await unitOfWork.SaveChangesAsync(ct); await scope.CommitAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<PurchaseResult>.Failure(new("Subscriptions.ConcurrentChange", "Purchase could not be confirmed. Retry with the same request key.", ErrorType.Conflict)); }
        return Result<PurchaseResult>.Success(new(Map(subscription, now), Map(payment), false));
    }
}
