using System.Security.Claims;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;

namespace BusinessLayer.Security;

public sealed class BlogOwnerRequirement : IAuthorizationRequirement;

public sealed class BlogOwnerAuthorizationHandler : AuthorizationHandler<BlogOwnerRequirement, Blog>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, BlogOwnerRequirement requirement, Blog resource)
    {
        if (context.User.Identity?.IsAuthenticated != true) return Task.CompletedTask;
        if (context.User.IsInRole(AppRoles.Admin) || context.User.IsInRole(AppRoles.Staff)
            || (context.User.IsInRole(AppRoles.Staff)
                && int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
                && resource.AppUserId == id))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
