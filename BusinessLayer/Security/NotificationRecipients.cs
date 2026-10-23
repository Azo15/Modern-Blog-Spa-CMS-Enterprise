using DataAccesLayer.Concrete;
using Microsoft.EntityFrameworkCore;
namespace BusinessLayer.Security;
public static class NotificationRecipients
{
    public static IQueryable<int> Query(AppDbContext db)
    {
        return (from user in db.Users where user.IsActive
            join membership in db.UserRoles on user.Id equals membership.UserId
            join role in db.Roles on membership.RoleId equals role.Id
            where role.Name == AppRoles.Admin || role.Name == AppRoles.Staff select user.Id).Distinct();
    }
    public static async Task<bool> CanReceiveAsync(AppDbContext db,int id,CancellationToken ct)
    {
        if (!await Query(db).AnyAsync(userId => userId == id,ct)) return false;
        var state = await db.Users.AsNoTracking().Where(u => u.Id == id).Select(u => new { u.LockoutEnabled,u.LockoutEnd }).SingleAsync(ct);
        return !state.LockoutEnabled || state.LockoutEnd == null || state.LockoutEnd <= DateTimeOffset.UtcNow;
    }
}
