using EntityLayer.Enum;
namespace BusinessLayer.Abstract;
public sealed record NotificationEvent(string EventKey, string Title, string Content, NotificationType Type, int? RelatedRecordId = null);
public sealed record NotificationItem(long Id, string Title, string Content, NotificationType Type, int? RelatedRecordId, bool IsRead, DateTime CreatedAtUtc, string Url);
public sealed record NotificationPage(IReadOnlyList<NotificationItem> Items, int UnreadCount, int TotalCount, long LatestId, int Page);
public interface INotificationService
{
    Task PublishAsync(NotificationEvent notification, CancellationToken ct = default);
    Task<NotificationPage> ListAsync(int userId, int page = 1, int size = 10, CancellationToken ct = default);
    Task<int> CountAsync(int userId, CancellationToken ct = default);
    Task<bool> ReadAsync(int userId, long id, CancellationToken ct = default);
    Task ReadAllAsync(int userId, long throughId, CancellationToken ct = default);
}
public interface INotificationRealtime { Task ChangedAsync(int userId, CancellationToken ct); }
