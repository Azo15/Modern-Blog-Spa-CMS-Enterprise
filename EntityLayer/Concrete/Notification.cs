using EntityLayer.Enum;
namespace EntityLayer.Concrete;
public sealed class Notification
{
    public long Id { get; set; }
    public int RecipientUserId { get; set; }
    public AppUser RecipientUser { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public NotificationType Type { get; set; }
    public int? RelatedRecordId { get; set; }
    public string EventKey { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAtUtc { get; set; }
}
