using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Application.Common.Abstractions.Time;
using NetflixClone.Application.Common.Results;
using NetflixClone.Domain.Constants;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Application.Admin.Users;
public interface IAdminUserUseCase
{
    Task<Result<AdminUserPage>> ListAsync(int actorId, UserSearch query, CancellationToken ct);
    Task<Result<AdminUser>> GetAsync(int actorId, int id, CancellationToken ct);
    Task<Result<IReadOnlyList<AdminUserRole>>> RolesAsync(int actorId, CancellationToken ct);
    Task<Result<AdminUser>> LockAsync(ChangeUserLock command, CancellationToken ct);
    Task<Result<AdminUser>> ChangeRolesAsync(ChangeUserRoles command, CancellationToken ct);
    Task<Result<AdminLogPage>> LogsAsync(int actorId, LogSearch query, CancellationToken ct);
}
public sealed class AdminUserUseCase(IAdminUserRepository users, IAdminUserMutationScopeFactory scopes,
    IUnitOfWork unitOfWork, IClock clock) : IAdminUserUseCase
{
    private static readonly Error Forbidden = new("AdminUsers.Forbidden", "Current administrator access is required.", ErrorType.Forbidden);
    private static readonly Error Invalid = new("AdminUsers.InvalidData", "Check the request. A reason (maximum 400 characters) and an exact UTC version are required for changes. Choose at least one supported role.", ErrorType.Validation);
    private static readonly Error Missing = new("AdminUsers.NotFound", "The account was not found.", ErrorType.NotFound);
    private static readonly Error Concurrent = new("AdminUsers.ConcurrentChange", "The account changed. Reload before trying again.", ErrorType.Conflict);
    private static readonly Error Self = new("AdminUsers.SelfProtection", "You cannot lock your own account or remove your own Admin role.", ErrorType.Conflict);
    private static readonly Error LastAdmin = new("AdminUsers.LastAdmin", "Keep at least one unlocked, email-confirmed administrator.", ErrorType.Conflict);
    private async Task<Error?> Access(int id, CancellationToken ct)
    {
        var actor = await users.GetAsync(id, ct);
        return actor is null || actor.IsLocked || !actor.EmailConfirmed || !actor.UserRoles.Any(r => r.Role.Name == RoleNames.Admin) ? Forbidden : null;
    }
    private static bool PageValid(int p, int s) => p > 0 && s is >= 1 and <= 50 && (long)(p - 1) * s <= int.MaxValue;
    public async Task<Result<AdminUserPage>> ListAsync(int actorId, UserSearch query, CancellationToken ct)
    {
        var access = await Access(actorId, ct);
        if (access is not null) return Result<AdminUserPage>.Failure(access);
        var search = query.Search?.Trim();
        if (!PageValid(query.Page, query.PageSize) || search?.Length > 100) return Result<AdminUserPage>.Failure(Invalid);
        return Result<AdminUserPage>.Success(await users.ListAsync(query with { Search = search }, ct));
    }
    public async Task<Result<AdminUser>> GetAsync(int actorId, int id, CancellationToken ct)
    {
        var access = await Access(actorId, ct);
        if (access is not null) return Result<AdminUser>.Failure(access);
        var target = await users.GetAsync(id, ct);
        return target is null ? Result<AdminUser>.Failure(Missing) : Result<AdminUser>.Success(Map(target));
    }
    public async Task<Result<IReadOnlyList<AdminUserRole>>> RolesAsync(int actorId, CancellationToken ct)
    {
        var access = await Access(actorId, ct);
        if (access is not null) return Result<IReadOnlyList<AdminUserRole>>.Failure(access);
        return Result<IReadOnlyList<AdminUserRole>>.Success((await users.RolesAsync(ct)).Select(r => new AdminUserRole(r.Id, r.Name)).ToArray());
    }
    private static string? Reason(string? reason) => string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    private static bool ValidChange(string? reason, DateTime? version) => reason is { Length: <= 400 } && version is { Kind: DateTimeKind.Utc };
    public async Task<Result<AdminUser>> LockAsync(ChangeUserLock command, CancellationToken ct)
    {
        var reason = Reason(command.Reason);
        if (!ValidChange(reason, command.ExpectedUpdatedAtUtc)) return Result<AdminUser>.Failure(Invalid);
        await using var scope = await scopes.BeginAsync(command.TargetId, ct);
        var access = await Access(command.ActorId, ct);
        if (access is not null) return Result<AdminUser>.Failure(access);
        var target = await users.GetAsync(command.TargetId, ct);
        if (target is null) return Result<AdminUser>.Failure(Missing);
        if (target.UpdatedAt.Ticks != command.ExpectedUpdatedAtUtc!.Value.Ticks) return Result<AdminUser>.Failure(Concurrent);
        if (command.IsLocked && command.TargetId == command.ActorId) return Result<AdminUser>.Failure(Self);
        if (target.IsLocked == command.IsLocked) return Result<AdminUser>.Success(Map(target));
        if (command.IsLocked && Eligible(target) && await users.CountEligibleAdminsAsync(ct) <= 1) return Result<AdminUser>.Failure(LastAdmin);
        var now = clock.UtcNow;
        target.IsLocked = command.IsLocked;
        Advance(target, now);
        Log(command.ActorId, target.Id, command.IsLocked ? UserAuditActions.Locked : UserAuditActions.Unlocked, reason!, now);
        await unitOfWork.SaveChangesAsync(ct);
        await scope.CommitAsync(ct);
        return Result<AdminUser>.Success(Map(target));
    }
    public async Task<Result<AdminUser>> ChangeRolesAsync(ChangeUserRoles command, CancellationToken ct)
    {
        var reason = Reason(command.Reason);
        if (!ValidChange(reason, command.ExpectedUpdatedAtUtc) || command.RoleIds is null || command.RoleIds.Count is < 1 or > 2 ||
            command.RoleIds.Any(id => id <= 0) || command.RoleIds.Distinct().Count() != command.RoleIds.Count)
            return Result<AdminUser>.Failure(Invalid);
        await using var scope = await scopes.BeginAsync(command.TargetId, ct);
        var access = await Access(command.ActorId, ct);
        if (access is not null) return Result<AdminUser>.Failure(access);
        var target = await users.GetAsync(command.TargetId, ct);
        if (target is null) return Result<AdminUser>.Failure(Missing);
        if (target.UpdatedAt.Ticks != command.ExpectedUpdatedAtUtc!.Value.Ticks) return Result<AdminUser>.Failure(Concurrent);
        var selected = (await users.RolesAsync(ct)).Where(r => command.RoleIds.Contains(r.Id)).ToArray();
        if (selected.Length != command.RoleIds.Count) return Result<AdminUser>.Failure(Invalid);
        var keepsAdmin = selected.Any(r => r.Name == RoleNames.Admin);
        if (command.ActorId == command.TargetId && !keepsAdmin) return Result<AdminUser>.Failure(Self);
        if (!keepsAdmin && Eligible(target) && await users.CountEligibleAdminsAsync(ct) <= 1) return Result<AdminUser>.Failure(LastAdmin);
        var now = clock.UtcNow; var changed = false;
        foreach (var old in target.UserRoles.ToArray())
        {
            if (command.RoleIds.Contains(old.RoleId)) continue;
            users.RemoveRole(old); target.UserRoles.Remove(old); changed = true;
            Log(command.ActorId, target.Id, UserAuditActions.Removed, $"Role: {old.Role.Name}. Reason: {reason}", now);
        }
        foreach (var role in selected)
        {
            if (target.UserRoles.Any(r => r.RoleId == role.Id)) continue;
            target.UserRoles.Add(new() { RoleId = role.Id, Role = role, AssignedAt = now }); changed = true;
            Log(command.ActorId, target.Id, UserAuditActions.Assigned, $"Role: {role.Name}. Reason: {reason}", now);
        }
        if (changed)
        {
            Advance(target, now);
            await unitOfWork.SaveChangesAsync(ct);
            await scope.CommitAsync(ct);
        }
        return Result<AdminUser>.Success(Map(target));
    }
    public async Task<Result<AdminLogPage>> LogsAsync(int actorId, LogSearch query, CancellationToken ct)
    {
        var access = await Access(actorId, ct);
        if (access is not null) return Result<AdminLogPage>.Failure(access);
        if (!PageValid(query.Page, query.PageSize) || query.ActorId is <= 0 || query.TargetId is <= 0 ||
            query.Action is not null && !UserAuditActions.IsValid(query.Action)) return Result<AdminLogPage>.Failure(Invalid);
        return Result<AdminLogPage>.Success(await users.LogsAsync(query, ct));
    }
    private static bool Eligible(UserAccount a) => !a.IsLocked && a.EmailConfirmed && a.UserRoles.Any(r => r.Role.Name == RoleNames.Admin);
    private static void Advance(UserAccount a, DateTime now) => a.UpdatedAt = now > a.UpdatedAt ? now : a.UpdatedAt.AddTicks(1);
    private void Log(int actor, int target, string action, string reason, DateTime now) => users.AddLog(new() {
        ActorUserAccountId = actor, TargetUserAccountId = target, Action = action, Reason = reason, CreatedAt = now });
    private static AdminUser Map(UserAccount a) => new(a.Id, a.Email, a.EmailConfirmed, a.IsLocked,
        DateTime.SpecifyKind(a.CreatedAt, DateTimeKind.Utc), DateTime.SpecifyKind(a.UpdatedAt, DateTimeKind.Utc),
        a.UserRoles.OrderBy(r => r.Role.Name).Select(r => new AdminUserRole(r.RoleId, r.Role.Name)).ToArray());
}
