using Microsoft.AspNetCore.SignalR;

namespace WebUI.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string title, string message, string type)
        {
            await Clients.All.SendAsync("ReceiveNotification", title, message, type);
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
