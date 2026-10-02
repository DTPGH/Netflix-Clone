using NetflixClone.Application.Admin.Users;
namespace NetflixClone.Api.Contracts.Admin.Users;
public sealed record RoleResponse(int RoleId, string Name);
public sealed record UserResponse(int UserAccountId, string Email, bool EmailConfirmed, bool IsLocked, DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc, IReadOnlyList<RoleResponse> Roles)
{
    public static UserResponse From(AdminUser a) => new(a.UserAccountId, a.Email, a.EmailConfirmed, a.IsLocked, a.CreatedAtUtc,
        a.UpdatedAtUtc, a.Roles.Select(r => new RoleResponse(r.RoleId, r.Name)).ToArray());
}
public sealed record UserPageResponse(IReadOnlyList<UserResponse> Items, int Page, int PageSize, int TotalCount);
public sealed record LockUserRequest(bool IsLocked, string? Reason, DateTime? ExpectedUpdatedAtUtc);
public sealed record ChangeRolesRequest(int[]? RoleIds, string? Reason, DateTime? ExpectedUpdatedAtUtc);
public sealed record LogResponse(int LogId, int ActorUserAccountId, string ActorEmail, int TargetUserAccountId,
    string TargetEmail, string Action, string? Reason, DateTime CreatedAtUtc);
public sealed record LogPageResponse(IReadOnlyList<LogResponse> Items, int Page, int PageSize, int TotalCount);
