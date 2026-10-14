using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BusinessLayer.Abstract;
using BusinessLayer.Dtos;
using EntityLayer.Concrete;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace WebUI.Api;

/// <summary>Issues signed, bounded lifetime JWTs with revocable Identity state.</summary>
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock) : IApiTokenIssuer
{
    public Task<ApiTokenDto> IssueAsync(AppUser user, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrEmpty(user.SecurityStamp)) throw new InvalidOperationException("The account requires an Identity security stamp.");
        var settings = options.Value;
        var now = clock.GetUtcNow();
        var expires = now.AddMinutes(settings.AccessTokenMinutes);
        var id = user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var distinctRoles = roles.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, id),
            new(ClaimTypes.NameIdentifier, id),
            new(ClaimTypes.Name, id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(ApiAuthentication.SecurityVersionClaim, SecurityVersion(user.SecurityStamp))
        };
        claims.AddRange(distinctRoles.Select(role => new Claim(ClaimTypes.Role, role)));
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, now.UtcDateTime, expires.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256));
        return Task.FromResult(new ApiTokenDto(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", expires, user.Id, distinctRoles));
    }

    public static string SecurityVersion(string securityStamp)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(securityStamp)));
}
