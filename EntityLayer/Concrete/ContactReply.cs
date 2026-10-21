namespace EntityLayer.Concrete;

/// <summary>Immutable reply and durable email intent. Mail retries never create another reply.</summary>
public sealed class ContactReply
{
    public int Id { get; set; }
    public int ContactFormId { get; set; }
    public ContactForm ContactForm { get; set; } = null!;
    public string ReplyMessage { get; set; } = "";
    public string RepliedByUserId { get; set; } = "";
    public string RepliedByUserName { get; set; } = "";
    public DateTime RepliedAt { get; set; } = DateTime.UtcNow;
    public bool IsMailSent { get; set; }
    public string RecipientEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public Guid ClientRequestId { get; set; }
    public string MessageId { get; set; } = $"{Guid.NewGuid():N}@xspa.local";
    public string? LeaseToken { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? LastEnqueuedAtUtc { get; set; }
    public string? HangfireJobId { get; set; }
    public string? LastError { get; set; }
    public int AttemptCount { get; set; }
}
