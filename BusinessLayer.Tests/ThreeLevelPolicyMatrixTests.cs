using BusinessLayer.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
namespace BusinessLayer.Tests;
public sealed class ThreeLevelPolicyMatrixTests
{
    [Theory]
    [InlineData(AccessPolicies.Admin, AppRoles.Admin, true)]
    [InlineData(AccessPolicies.Admin, AppRoles.Staff, false)]
    [InlineData(AccessPolicies.Admin, AppRoles.Customer, false)]
    [InlineData(AccessPolicies.ContentManager, AppRoles.Admin, true)]
    [InlineData(AccessPolicies.ContentManager, AppRoles.Staff, true)]
    [InlineData(AccessPolicies.ContentManager, AppRoles.Customer, false)]
    [InlineData(AccessPolicies.Moderator, AppRoles.Staff, true)]
    [InlineData(AccessPolicies.Moderator, AppRoles.Customer, false)]
    [InlineData(AccessPolicies.Member, AppRoles.Customer, true)]
    [InlineData(AccessPolicies.Member, AppRoles.Staff, true)]
    [InlineData(AccessPolicies.Member, AppRoles.Admin, true)]
    [InlineData(AccessPolicies.Member, "Author", false)]
    public async Task PoliciesEnforceTheThreeLevelBoundary(string policy,string role,bool expected)
    {
        using var services=new ServiceCollection().AddLogging().AddAuthorization(AccessPolicies.Configure).BuildServiceProvider();
        var user=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Role,role)},"test"));
        var result=await services.GetRequiredService<IAuthorizationService>().AuthorizeAsync(user,policy);
        Assert.Equal(expected,result.Succeeded);
        Assert.False((await services.GetRequiredService<IAuthorizationService>().AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()),policy)).Succeeded);
    }
}
