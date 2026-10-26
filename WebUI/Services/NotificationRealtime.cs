using BusinessLayer.Abstract;
using BusinessLayer.Security;
using DataAccesLayer.Concrete;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebUI.Hubs;
namespace WebUI.Services;
public sealed class NotificationRealtime(AppDbContext db,IHubContext<NotificationHub> hub) : INotificationRealtime
{
    public async Task ChangedAsync(int userId,CancellationToken ct)
    {
        if (!await NotificationRecipients.CanReceiveAsync(db,userId,ct)) return;
        var count = await db.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead,ct);
        await hub.Clients.Group(NotificationHub.RecipientGroup(userId)).SendAsync("NotificationsChanged",new { unreadCount = count },ct);
    }
}
