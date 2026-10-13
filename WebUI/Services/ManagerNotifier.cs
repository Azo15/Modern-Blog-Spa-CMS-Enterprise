using BusinessLayer.Abstract;
using Microsoft.AspNetCore.SignalR;
using WebUI.Hubs;

namespace WebUI.Services;

public sealed class ManagerNotifier(IHubContext<NotificationHub> hub, ILogger<ManagerNotifier> logger) : IManagerNotifier
{
    public async Task NotifyAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        try { await hub.Clients.Group(NotificationHub.ModerationGroup).SendAsync("ReceiveNotification", title, message, "info", cancellationToken); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        { logger.LogWarning(exception, "Yorum kaydedildi ancak canlı bildirim gönderilemedi."); }
    }
}
