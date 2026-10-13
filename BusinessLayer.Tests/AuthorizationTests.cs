using System.Security.Claims;
using BusinessLayer.Security;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer.Tests;

public class AuthorizationTests
{
    internal static ClaimsPrincipal Principal(string? role, string? id = "1", bool authenticated = true)
    {
        var claims = new List<Claim>();
        if (role != null) claims.Add(new Claim(ClaimTypes.Role, role));
        if (id != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, id));
        claims.Add(new Claim(ClaimTypes.Email, "test@example.com"));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticated ? "Test" : null));
    }

    internal static ServiceProvider Provider()
    {
        var services = new ServiceCollection().AddLogging().AddAuthorization(AccessPolicies.Configure);
        services.AddSingleton<IAuthorizationHandler, BlogOwnerAuthorizationHandler>();
        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(AppRoles.Admin, true, true, true, true)]
    [InlineData(AppRoles.ContentManager, false, true, true, true)]
    [InlineData(AppRoles.Author, false, false, false, true)]
    [InlineData(AppRoles.Moderator, false, false, true, false)]
    [InlineData(AppRoles.RegisteredUser, false, false, false, false)]
    [InlineData(null, false, false, false, false)]
    public async Task PoliciesEnforceRoleMatrix(string? role, bool admin, bool content, bool moderation, bool author)
    {
        using var provider = Provider();
        var service = provider.GetRequiredService<IAuthorizationService>();
        var user = Principal(role, authenticated: role != null);
        Assert.Equal(admin, (await service.AuthorizeAsync(user, AccessPolicies.Admin)).Succeeded);
        Assert.Equal(content, (await service.AuthorizeAsync(user, AccessPolicies.ContentManager)).Succeeded);
        Assert.Equal(moderation, (await service.AuthorizeAsync(user, AccessPolicies.Moderator)).Succeeded);
        Assert.Equal(author, (await service.AuthorizeAsync(user, AccessPolicies.BlogAuthor)).Succeeded);
        Assert.True((await service.AuthorizeAsync(user, AccessPolicies.Visitor)).Succeeded);
    }

    [Theory]
    [InlineData(AppRoles.Author, "1", true)]
    [InlineData(AppRoles.Author, "2", false)]
    [InlineData(AppRoles.Author, "invalid", false)]
    [InlineData(AppRoles.Author, null, false)]
    [InlineData(AppRoles.ContentManager, "2", true)]
    [InlineData(AppRoles.Admin, "2", true)]
    [InlineData(AppRoles.Moderator, "1", false)]
    [InlineData(AppRoles.RegisteredUser, "1", false)]
    public async Task ResourcePolicyRequiresOwnershipOrManagement(string role, string? id, bool allowed)
    {
        using var provider = Provider();
        var service = provider.GetRequiredService<IAuthorizationService>();
        Assert.Equal(allowed, (await service.AuthorizeAsync(Principal(role, id),
            new Blog { AppUserId = 1 }, AccessPolicies.BlogResource)).Succeeded);
    }

    [Fact]
    public async Task AnonymousIdentityCannotSpoofAnAdminRole()
    {
        using var provider = Provider();
        var service = provider.GetRequiredService<IAuthorizationService>();
        Assert.False((await service.AuthorizeAsync(Principal(AppRoles.Admin, authenticated: false),
            new Blog { AppUserId = 1 }, AccessPolicies.BlogResource)).Succeeded);
    }
}
