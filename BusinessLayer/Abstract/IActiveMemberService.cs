using System.Security.Claims;

namespace BusinessLayer.Abstract;

public interface IActiveMemberService
{
    Task<bool> IsActiveAsync(ClaimsPrincipal principal, CancellationToken ct = default);
    Task<int> RequireAsync(CancellationToken ct = default);
}
