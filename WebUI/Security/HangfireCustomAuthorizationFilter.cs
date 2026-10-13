using BusinessLayer.Security;
using Hangfire.Dashboard;

namespace WebUI.Security;

public sealed class HangfireCustomAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var user = context.GetHttpContext().User;
        return user.Identity?.IsAuthenticated == true && user.IsInRole(AppRoles.Admin);
    }
}
