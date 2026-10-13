using System.Security.Claims;
using DataAccesLayer.Abstract;

namespace WebUI.Services;

public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    public ClaimsPrincipal User => accessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
    public string? UserId => User.Identity?.IsAuthenticated == true
        ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
    public string? Email => User.Identity?.IsAuthenticated == true
        ? User.FindFirstValue(ClaimTypes.Email) : null;
}
