using DataAccesLayer.Concrete;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebUI.Hubs;
using WebUI.Services;
namespace BusinessLayer.Tests;
public sealed class NotificationRealtimeTests
{
    [Fact]
    public async Task CountUpdatesGoOnlyToPersonalManagerGroupAndRevokedMembersReceiveNothing()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var scope = f.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var proxy = new Mock<IClientProxy>(); proxy.Setup(x => x.SendCoreAsync(It.IsAny<string>(),It.IsAny<object?[]>(),It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var clients = new Mock<IHubClients>(); clients.Setup(x => x.Group(NotificationHub.RecipientGroup(1))).Returns(proxy.Object);
        var hub = new Mock<IHubContext<NotificationHub>>(); hub.SetupGet(x => x.Clients).Returns(clients.Object);
        var realtime = new NotificationRealtime(db,hub.Object); await realtime.ChangedAsync(1,default);
        proxy.Verify(x => x.SendCoreAsync("NotificationsChanged",It.IsAny<object?[]>(),It.IsAny<CancellationToken>()),Times.Once);
        await realtime.ChangedAsync(5,default);
        await db.Users.Where(x => x.Id == 1).ExecuteUpdateAsync(u => u.SetProperty(x => x.IsActive,false));
        await realtime.ChangedAsync(1,default); clients.Verify(x => x.Group(It.IsAny<string>()),Times.Once); clients.VerifyNoOtherCalls();
    }
}
