namespace EntityLayer.Concrete;

/// <summary>Durable receipt intent saved atomically with the appointment.</summary>
public sealed class AppointmentEmailDelivery
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string MessageId { get; set; } = $"{Guid.NewGuid():N}@xspa.local";
    public DateTime? SentAtUtc { get; set; }
    public string? LeaseToken { get; set; }
    public DateTime? LeaseExpiresAtUtc { get; set; }
    public string? HangfireJobId { get; set; }
    public DateTime? LastEnqueuedAtUtc { get; set; }
    public string? LastError { get; set; }
    public int AttemptCount { get; set; }
}
