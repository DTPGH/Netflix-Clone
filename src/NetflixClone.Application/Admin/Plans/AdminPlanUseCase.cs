using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Exceptions;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Admin.Plans;
public interface IAdminPlanUseCase
{
    Task<Result<IReadOnlyList<AdminPlan>>> ListAsync(int actorId, CancellationToken ct);
    Task<Result<AdminPlan>> GetAsync(int actorId, int id, CancellationToken ct);
    Task<Result<AdminPlan>> SaveAsync(SaveAdminPlan command, CancellationToken ct);
    Task<Result<AdminPlan>> StatusAsync(int actorId, int id, bool isActive, DateTime? expectedUpdatedAtUtc, CancellationToken ct);
}
public sealed class AdminPlanUseCase(IAdminPlanRepository plans, IAdminPlanMutationScopeFactory scopes,
    IUserAccountRepository accounts, IUnitOfWork unitOfWork, IClock clock) : IAdminPlanUseCase
{
    private static readonly Error Invalid = new("AdminPlans.InvalidData", "Enter a name (maximum 100 characters), a non-negative price with at most two decimal places, 1–10 streams and a supported quality. Updates require an exact UTC version.", ErrorType.Validation);
    private static readonly Error Missing = new("AdminPlans.NotFound", "The plan was not found.", ErrorType.NotFound);
    private static readonly Error Duplicate = new("AdminPlans.DuplicateName", "A plan with this name already exists.", ErrorType.Conflict);
    private static readonly Error Changed = new("AdminPlans.ConcurrentChange", "The plan changed. Reload before trying again.", ErrorType.Conflict);
    private static readonly Error Used = new("AdminPlans.InUse", "This plan has subscription history. Create a new plan instead of changing its configuration.", ErrorType.Conflict);
    private async Task<Error?> Access(int actorId, CancellationToken ct)
    {
        var actor = await accounts.GetByIdAsync(actorId, ct);
        return actor is null || actor.IsLocked || !actor.EmailConfirmed ||
            !(await accounts.GetRoleNamesAsync(actorId, ct)).Contains(RoleNames.Admin, StringComparer.Ordinal)
            ? new Error("AdminPlans.Forbidden", "Current administrator access is required.", ErrorType.Forbidden) : null;
    }
    public async Task<Result<IReadOnlyList<AdminPlan>>> ListAsync(int actorId, CancellationToken ct)
    {
        var access = await Access(actorId, ct);
        return access is not null ? Result<IReadOnlyList<AdminPlan>>.Failure(access)
            : Result<IReadOnlyList<AdminPlan>>.Success(await plans.ListAsync(ct));
    }
    public async Task<Result<AdminPlan>> GetAsync(int actorId, int id, CancellationToken ct)
    {
        var access = await Access(actorId, ct);
        if (access is not null) return Result<AdminPlan>.Failure(access);
        var plan = await plans.GetAsync(id, ct);
        return plan is null ? Result<AdminPlan>.Failure(Missing) : Result<AdminPlan>.Success(Map(plan, await plans.SubscriptionCountAsync(id, ct)));
    }
    public async Task<Result<AdminPlan>> SaveAsync(SaveAdminPlan command, CancellationToken ct)
    {
        var access = await Access(command.ActorId, ct);
        if (access is not null) return Result<AdminPlan>.Failure(access);
        var name = command.Name?.Trim(); var quality = command.MaxQuality?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 100 || command.Price < 0 || command.Price > 9999999999999999.99m ||
            decimal.Round(command.Price, 2) != command.Price || command.MaxConcurrentStreams is < 1 or > 10 ||
            quality is not ("480p" or "720p" or "1080p" or "4K"))
            return Result<AdminPlan>.Failure(Invalid);
        await using var scope = await scopes.BeginAsync(ct);
        Plan plan;
        if (command.PlanId is { } id)
        {
            if (command.ExpectedUpdatedAtUtc is not { Kind: DateTimeKind.Utc }) return Result<AdminPlan>.Failure(Invalid);
            var existing = await plans.GetAsync(id, ct);
            if (existing is null) return Result<AdminPlan>.Failure(Missing);
            if (existing.UpdatedAt.Ticks != command.ExpectedUpdatedAtUtc.Value.Ticks) return Result<AdminPlan>.Failure(Changed);
            if (await plans.SubscriptionCountAsync(id, ct) > 0) return Result<AdminPlan>.Failure(Used);
            plan = existing;
        }
        else plan = new() { IsActive = false, CreatedAt = clock.UtcNow, UpdatedAt = clock.UtcNow };
        if (await plans.NameExistsAsync(name, command.PlanId, ct)) return Result<AdminPlan>.Failure(Duplicate);
        plan.Name = name; plan.Price = command.Price; plan.MaxConcurrentStreams = command.MaxConcurrentStreams; plan.MaxQuality = quality;
        if (command.PlanId.HasValue) Advance(plan);
        else await plans.AddAsync(plan, ct);
        try { await unitOfWork.SaveChangesAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<AdminPlan>.Failure(Changed); }
        await scope.CommitAsync(ct);
        return Result<AdminPlan>.Success(Map(plan, 0));
    }
    public async Task<Result<AdminPlan>> StatusAsync(int actorId, int id, bool isActive, DateTime? expectedUpdatedAtUtc, CancellationToken ct)
    {
        var access = await Access(actorId, ct);
        if (access is not null) return Result<AdminPlan>.Failure(access);
        if (expectedUpdatedAtUtc is not { Kind: DateTimeKind.Utc }) return Result<AdminPlan>.Failure(Invalid);
        await using var scope = await scopes.BeginAsync(ct);
        var plan = await plans.GetAsync(id, ct);
        if (plan is null) return Result<AdminPlan>.Failure(Missing);
        if (plan.UpdatedAt.Ticks != expectedUpdatedAtUtc.Value.Ticks) return Result<AdminPlan>.Failure(Changed);
        var count = await plans.SubscriptionCountAsync(id, ct);
        if (plan.IsActive == isActive) return Result<AdminPlan>.Success(Map(plan, count));
        plan.IsActive = isActive; Advance(plan);
        try { await unitOfWork.SaveChangesAsync(ct); }
        catch (PersistenceConcurrencyException) { return Result<AdminPlan>.Failure(Changed); }
        await scope.CommitAsync(ct);
        return Result<AdminPlan>.Success(Map(plan, count));
    }
    private void Advance(Plan p) { var now = clock.UtcNow; p.UpdatedAt = now > p.UpdatedAt ? now : p.UpdatedAt.AddTicks(1); }
    private static AdminPlan Map(Plan p, int count) => new(p.Id, p.Name, p.Price, p.MaxConcurrentStreams, p.MaxQuality, p.IsActive,
        count, DateTime.SpecifyKind(p.CreatedAt, DateTimeKind.Utc), DateTime.SpecifyKind(p.UpdatedAt, DateTimeKind.Utc));
}
