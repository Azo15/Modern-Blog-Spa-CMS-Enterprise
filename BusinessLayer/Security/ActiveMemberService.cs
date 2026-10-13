using System.Security.Claims;
using BusinessLayer.Abstract;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Security;

// Read database state on every mutation: an already-issued cookie must not bypass lockout.
public sealed class ActiveMemberService(AppDbContext db, ICurrentUserService currentUser) : IActiveMemberService
{
    public async Task<bool> IsActiveAsync(ClaimsPrincipal principal, CancellationToken ct = default)
    {
        if (principal.Identity?.IsAuthenticated != true || !AppRoles.All.Any(principal.IsInRole) ||
            !int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return false;
        var account = await db.Users.AsNoTracking().Where(u => u.Id == id)
            .Select(u => new { u.IsActive, u.LockoutEnabled, u.LockoutEnd }).SingleOrDefaultAsync(ct);
        return account is { IsActive: true } && !(account.LockoutEnabled && account.LockoutEnd > DateTimeOffset.UtcNow);
    }

    public async Task<int> RequireAsync(CancellationToken ct = default)
    {
        if (!await IsActiveAsync(currentUser.User, ct))
            throw new UnauthorizedAccessException("Hesap engellenmiş veya oturum geçersiz.");
        return int.Parse(currentUser.UserId!);
    }
}

public sealed class ActiveMemberRequirement : IAuthorizationRequirement;

public sealed class ActiveMemberAuthorizationHandler(IActiveMemberService members) : AuthorizationHandler<ActiveMemberRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ActiveMemberRequirement requirement)
    {
        if (await members.IsActiveAsync(context.User)) context.Succeed(requirement);
    }
}
