namespace EntityLayer.Concrete;

/// <summary>Durable delivery intent; deliberately not soft-deletable so delivery history survives trash operations.</summary>
public sealed class ContactEmailDelivery
{
    public int Id { get; set; }
    public int ContactFormId { get; set; }
    public ContactForm ContactForm { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string MessageId { get; set; } = $"{Guid.NewGuid():N}@modern-blog.local";
    public DateTime? SentAtUtc { get; set; }
    public string? LeaseToken { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public string? HangfireJobId { get; set; }
    public DateTime? LastEnqueuedAtUtc { get; set; }
    public string? LastError { get; set; }
    public int AttemptCount { get; set; }
}
