using System.Security.Claims;

namespace DataAccesLayer.Abstract;

public interface ICurrentUserService
{
    ClaimsPrincipal User { get; }
    string? UserId { get; }
    string? Email { get; }
}
