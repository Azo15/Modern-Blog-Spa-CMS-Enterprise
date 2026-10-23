using Microsoft.AspNetCore.SignalR;
using DataAccesLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using BusinessLayer.Security;

namespace WebUI.Hubs
{
    [Authorize(Policy = AccessPolicies.Moderator)]
    public class NotificationHub(AppDbContext db) : Hub
    {
        public const string ModerationGroup = "Managers";

        public static string RecipientGroup(int userId) => $"notifications:user:{userId}";

        public override async Task OnConnectedAsync()
        {
            if (!int.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) || !await NotificationRecipients.CanReceiveAsync(db,id,Context.ConnectionAborted))
                throw new HubException("Bildirim erişimi yok.");
            await Groups.AddToGroupAsync(Context.ConnectionId, RecipientGroup(id));
            await Groups.AddToGroupAsync(Context.ConnectionId, ModerationGroup);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
