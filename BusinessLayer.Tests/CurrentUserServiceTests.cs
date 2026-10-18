using BusinessLayer.Security;
using Microsoft.AspNetCore.Http;
using WebUI.Services;

namespace BusinessLayer.Tests;

public class CurrentUserServiceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void IdAndEmailOnlyComeFromAuthenticatedIdentity(bool authenticated)
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext {
            User = AuthorizationTests.Principal(AppRoles.Staff, "7", authenticated) } };
        var service = new CurrentUserService(accessor);
        Assert.Equal(authenticated ? "7" : null, service.UserId);
        Assert.Equal(authenticated ? "test@example.com" : null, service.Email);
    }

    [Fact]
    public void BackgroundWorkHasNoFabricatedUser()
    {
        var service = new CurrentUserService(new HttpContextAccessor());
        Assert.Null(service.UserId);
        Assert.Null(service.Email);
        Assert.False(service.User.Identity?.IsAuthenticated == true);
    }
}
