using Microsoft.EntityFrameworkCore;
using NetflixClone.Application.Admin.Users;
using NetflixClone.Application.Common.Abstractions.Persistence;
using NetflixClone.Domain.Constants;
using NetflixClone.Domain.Entities;
namespace NetflixClone.Infrastructure.Persistence.Repositories;
public sealed class AdminUserRepository(NetflixCloneDbContext db) : IAdminUserRepository
{
    public async Task<AdminUserPage> ListAsync(UserSearch search, CancellationToken ct)
    {
        var query = db.UserAccounts.AsNoTracking();
        if (!string.IsNullOrEmpty(search.Search)) query = query.Where(a => a.Email.Contains(search.Search));
        if (search.IsLocked is { } locked) query = query.Where(a => a.IsLocked == locked);
        var count = await query.CountAsync(ct);
        var items = await query.OrderBy(a => a.Id).Skip((search.Page - 1) * search.PageSize).Take(search.PageSize)
            .Select(a => new AdminUser(a.Id, a.Email, a.EmailConfirmed, a.IsLocked, a.CreatedAt, a.UpdatedAt,
                a.UserRoles.OrderBy(r => r.Role.Name).Select(r => new AdminUserRole(r.RoleId, r.Role.Name)).ToArray())).ToListAsync(ct);
        return new(items.Select(a => a with { CreatedAtUtc = Utc(a.CreatedAtUtc), UpdatedAtUtc = Utc(a.UpdatedAtUtc) }).ToArray(), search.Page, search.PageSize, count);
    }
    public Task<UserAccount?> GetAsync(int id, CancellationToken ct) => db.UserAccounts.Include(a => a.UserRoles).ThenInclude(r => r.Role).SingleOrDefaultAsync(a => a.Id == id, ct);
    public async Task<IReadOnlyList<Role>> RolesAsync(CancellationToken ct) => await db.Roles
        .Where(r => r.Name == RoleNames.Admin || r.Name == RoleNames.User).OrderBy(r => r.Name).ToListAsync(ct);
    public Task<int> CountEligibleAdminsAsync(CancellationToken ct) => db.UserAccounts.CountAsync(a => !a.IsLocked && a.EmailConfirmed && a.UserRoles.Any(r => r.Role.Name == RoleNames.Admin), ct);
    public void RemoveRole(UserRole role) => db.UserRoles.Remove(role);
    public void AddLog(AdminActionLog log) => db.AdminActionLogs.Add(log);
    public async Task<AdminLogPage> LogsAsync(LogSearch search, CancellationToken ct)
    {
        var query = db.AdminActionLogs.AsNoTracking();
        if (search.ActorId is { } actor) query = query.Where(l => l.ActorUserAccountId == actor);
        if (search.TargetId is { } target) query = query.Where(l => l.TargetUserAccountId == target);
        if (search.Action is { } action) query = query.Where(l => l.Action == action);
        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id)
            .Skip((search.Page - 1) * search.PageSize).Take(search.PageSize)
            .Select(l => new AdminLog(l.Id, l.ActorUserAccountId, l.ActorUserAccount.Email, l.TargetUserAccountId,
                l.TargetUserAccount.Email, l.Action, l.Reason, l.CreatedAt)).ToListAsync(ct);
        return new(items.Select(l => l with { CreatedAtUtc = Utc(l.CreatedAtUtc) }).ToArray(), search.Page, search.PageSize, count);
    }
    private static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
