using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using BusinessLayer.Security;

namespace WebUI.Hubs
{
    [Authorize(Policy = AccessPolicies.Moderator)]
    public class NotificationHub : Hub
    {
        public const string ModerationGroup = "moderation-notifications";

        public override async Task OnConnectedAsync()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ModerationGroup);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
