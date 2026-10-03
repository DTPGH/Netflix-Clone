using NetflixClone.Application.Subscriptions;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Common.Abstractions.Persistence;
public interface ISubscriptionRepository
{
    Task<bool> HasEffectiveAsync(int accountId, DateTime utcNow, CancellationToken ct);
    Task<IReadOnlyList<AvailablePlan>> ListPlansAsync(CancellationToken ct);
    Task<Plan?> GetPlanForPurchaseAsync(int planId, CancellationToken ct);
    Task<PaymentTransaction?> FindPaymentAsync(int accountId, string transactionCode, CancellationToken ct);
    Task<IReadOnlyList<Subscription>> GetActiveRowsAsync(int accountId, CancellationToken ct);
    Task<Subscription?> GetLatestAsync(int accountId, CancellationToken ct);
    Task<PaymentPage> ListPaymentsAsync(int accountId, int page, CancellationToken ct);
    void Add(Subscription subscription, PaymentTransaction payment);
}
public interface ISubscriptionPurchaseScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct);
}
public interface ISubscriptionPurchaseScopeFactory
{
    Task<ISubscriptionPurchaseScope?> BeginAsync(int accountId, CancellationToken ct);
}
