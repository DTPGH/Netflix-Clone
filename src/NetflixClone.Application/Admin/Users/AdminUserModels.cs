namespace NetflixClone.Application.Admin.Users;
public sealed record AdminUserRole(int RoleId, string Name);
public sealed record AdminUser(int UserAccountId, string Email, bool EmailConfirmed, bool IsLocked,
    DateTime CreatedAtUtc, DateTime UpdatedAtUtc, IReadOnlyList<AdminUserRole> Roles);
public sealed record AdminUserPage(IReadOnlyList<AdminUser> Items, int Page, int PageSize, int TotalCount);
public sealed record AdminLog(int LogId, int ActorUserAccountId, string ActorEmail, int TargetUserAccountId,
    string TargetEmail, string Action, string? Reason, DateTime CreatedAtUtc);
public sealed record AdminLogPage(IReadOnlyList<AdminLog> Items, int Page, int PageSize, int TotalCount);
public sealed record UserSearch(int Page, int PageSize, string? Search, bool? IsLocked);
public sealed record LogSearch(int Page, int PageSize, int? ActorId, int? TargetId, string? Action);
public sealed record ChangeUserLock(int ActorId, int TargetId, bool IsLocked, string? Reason, DateTime? ExpectedUpdatedAtUtc);
public sealed record ChangeUserRoles(int ActorId, int TargetId, IReadOnlyList<int>? RoleIds, string? Reason, DateTime? ExpectedUpdatedAtUtc);
public static class UserAuditActions
{
    public const string Locked = "AccountLocked", Unlocked = "AccountUnlocked", Assigned = "RoleAssigned", Removed = "RoleRemoved";
    public static bool IsValid(string action) => action is Locked or Unlocked or Assigned or Removed;
}
