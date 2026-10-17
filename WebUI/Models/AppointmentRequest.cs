namespace WebUI.Models;

// Only visitor-editable fields are bound. Audit, moderation, reference and delivery fields stay server-owned.
public sealed class AppointmentRequest
{
    public int ServiceId { get; set; }
    public DateOnly AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int GuestCount { get; set; } = 1;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? GiftCode { get; set; }
    public Guid ClientRequestId { get; set; }
}
