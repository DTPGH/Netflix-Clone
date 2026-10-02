using System.ComponentModel.DataAnnotations;
namespace NetflixClone.Web.Client.Models;
public sealed record AdminUserRole(int RoleId, string Name);
public sealed record AdminUser(int UserAccountId, string Email, bool EmailConfirmed, bool IsLocked, DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc, AdminUserRole[] Roles);
public sealed record AdminUserPage(AdminUser[] Items, int Page, int PageSize, int TotalCount);
public sealed record AdminLog(int LogId, int ActorUserAccountId, string ActorEmail, int TargetUserAccountId,
    string TargetEmail, string Action, string? Reason, DateTime CreatedAtUtc);
public sealed record AdminLogPage(AdminLog[] Items, int Page, int PageSize, int TotalCount);
public sealed class AdminUserChangeForm : IValidatableObject
{
    [Required, MaxLength(400)] public string Reason { get; set; } = "";
    public HashSet<int> RoleIds { get; } = [];
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(Reason)) yield return new("Enter a reason for the audit log.", [nameof(Reason)]);
        if (RoleIds.Count == 0) yield return new("Keep at least one role.", [nameof(RoleIds)]);
    }
}
