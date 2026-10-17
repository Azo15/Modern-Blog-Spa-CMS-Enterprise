using EntityLayer.Enum;

namespace EntityLayer.Concrete;

public sealed class Appointment : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public string ServiceTitleSnapshot { get; set; } = string.Empty;
    public DateOnly AppointmentDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int GuestCount { get; set; } = 1;
    public string? Notes { get; set; }
    public string? GiftCode { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string ReferenceCode { get; set; } = string.Empty;
    // A browser retains this token when retrying a request whose response was lost.
    public Guid ClientRequestId { get; set; }
}
