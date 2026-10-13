using BusinessLayer.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebUI.Hubs;
namespace BusinessLayer.Tests;
public class NotificationHubTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData(AppRoles.RegisteredUser, false)]
    [InlineData(AppRoles.Author, false)]
    [InlineData(AppRoles.Moderator, true)]
    [InlineData(AppRoles.ContentManager, true)]
    [InlineData(AppRoles.Admin, true)]
    public async Task HubPolicyRejectsAnonymousAndUnauthorizedRoles(string? role, bool allowed)
    {
        var attribute = typeof(NotificationHub).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Single(); Assert.Equal(AccessPolicies.Moderator, attribute.Policy);
        using var provider = AuthorizationTests.Provider(); var auth = provider.GetRequiredService<IAuthorizationService>(); Assert.Equal(allowed, (await auth.AuthorizeAsync(AuthorizationTests.Principal(role, authenticated: role != null), attribute.Policy!)).Succeeded);
        Assert.Empty(typeof(NotificationHub).GetMethods(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Where(m => !m.IsSpecialName && !m.Name.StartsWith("On")));
    }
    [Fact]
    public async Task AuthorizedConnectionJoinsOnlyManagersGroup()
    {
        var groups = new Mock<IGroupManager>(); groups.Setup(g => g.AddToGroupAsync("connection-1", "Managers", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var context = new Mock<HubCallerContext>(); context.SetupGet(c => c.ConnectionId).Returns("connection-1"); context.SetupGet(c => c.User).Returns(AuthorizationTests.Principal(AppRoles.Moderator));
        var hub = new NotificationHub { Groups = groups.Object, Context = context.Object }; await hub.OnConnectedAsync(); groups.Verify(g => g.AddToGroupAsync("connection-1", "Managers", It.IsAny<CancellationToken>()), Times.Once);
    }
}
