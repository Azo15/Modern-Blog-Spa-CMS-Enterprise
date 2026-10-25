using BusinessLayer.Abstract;
using BusinessLayer.Security;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace BusinessLayer.Concrete;
public sealed class NoNotificationRealtime : INotificationRealtime { public Task ChangedAsync(int userId, CancellationToken ct) => Task.CompletedTask; }
public sealed class NotificationService(DbContextOptions<AppDbContext> options, INotificationRealtime realtime, ILogger<NotificationService> logger) : INotificationService
{
    public async Task PublishAsync(NotificationEvent notification, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(notification.EventKey) || notification.EventKey.Length > 200) throw new ArgumentException("Invalid event key.");
        await using var db = new AppDbContext(options);
        var recipients = await NotificationRecipients.Query(db).ToListAsync(ct);
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var existing = await db.Notifications.Where(n => n.EventKey == notification.EventKey).Select(n => n.RecipientUserId).ToListAsync(ct);
            var targets = recipients.Except(existing).ToArray(); if (targets.Length == 0) return;
            foreach (var id in targets) db.Notifications.Add(new Notification { RecipientUserId = id, EventKey = notification.EventKey, Title = Cut(notification.Title, 200), Content = Cut(notification.Content, 1000), Type = notification.Type, RelatedRecordId = notification.RelatedRecordId });
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException) when (attempt < 2) { db.ChangeTracker.Clear(); continue; }
            foreach (var id in targets) await PushAsync(id, ct);
            return;
        }
    }
    private static string Cut(string value, int length) => value.Length <= length ? value : value[..length];
    private async Task PushAsync(int id, CancellationToken ct)
    {
        try { await realtime.ChangedAsync(id, ct); }
        catch (Exception error) when (error is not OperationCanceledException) { logger.LogWarning("Notification push unavailable ({ErrorType}).", error.GetType().Name); }
    }
    public async Task<int> CountAsync(int userId, CancellationToken ct = default)
    { await using var db = new AppDbContext(options); return await db.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead, ct); }
    public async Task<NotificationPage> ListAsync(int userId, int page = 1, int size = 10, CancellationToken ct = default)
    {
        await using var db = new AppDbContext(options); var query = db.Notifications.AsNoTracking().Where(n => n.RecipientUserId == userId);
        var count = await query.CountAsync(ct); size = Math.Clamp(size, 1, 50); page = Math.Clamp(page, 1, Math.Max(1, (count + size - 1) / size));
        var unread = await query.CountAsync(n => !n.IsRead, ct); var latest = await query.Select(n => (long?)n.Id).MaxAsync(ct) ?? 0;
        var rows = await query.OrderByDescending(n => n.Id).Skip((page - 1) * size).Take(size).ToListAsync(ct);
        return new(rows.Select(n => new NotificationItem(n.Id,n.Title,n.Content,n.Type,n.RelatedRecordId,n.IsRead,n.CreatedAtUtc,Destination(n.Type,n.RelatedRecordId))).ToList(),unread,count,latest,page);
    }
    public async Task<bool> ReadAsync(int userId, long id, CancellationToken ct = default)
    {
        await using var db = new AppDbContext(options); var query = db.Notifications.Where(n => n.Id == id && n.RecipientUserId == userId);
        if (!await query.AnyAsync(ct)) return false;
        await query.Where(n => !n.IsRead).ExecuteUpdateAsync(u => u.SetProperty(n => n.IsRead,true).SetProperty(n => n.ReadAtUtc,DateTime.UtcNow),ct);
        await PushAsync(userId,ct); return true;
    }
    public async Task ReadAllAsync(int userId, long throughId, CancellationToken ct = default)
    {
        await using var db = new AppDbContext(options);
        await db.Notifications.Where(n => n.RecipientUserId == userId && !n.IsRead && n.Id <= throughId).ExecuteUpdateAsync(u => u.SetProperty(n => n.IsRead,true).SetProperty(n => n.ReadAtUtc,DateTime.UtcNow),ct);
        await PushAsync(userId,ct);
    }
    public static string Destination(NotificationType type, int? id) => type switch
    {
        NotificationType.ContactMessage or NotificationType.MessageReminder =>
            id is > 0 ? $"/AdminMessage/Details/{id}" : "/AdminMessage",
        NotificationType.AppointmentRequest =>
            id is > 0 ? $"/AdminAppointment?highlight={id}" : "/AdminAppointment",
        NotificationType.EmailFailure or NotificationType.SystemFailure =>
            "/hangfire",
        _ => "/AdminDashboard"
    };
}
