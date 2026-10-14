using BusinessLayer.Dtos;
using EntityLayer.Concrete;

namespace BusinessLayer.Abstract;

/// <summary>Creates short lived API credentials after Identity verifies the account.</summary>
public interface IApiTokenIssuer
{
    Task<ApiTokenDto> IssueAsync(AppUser user, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default);
}
