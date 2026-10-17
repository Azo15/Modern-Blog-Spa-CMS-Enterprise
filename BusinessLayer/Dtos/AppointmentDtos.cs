using EntityLayer.Enum;

namespace BusinessLayer.Dtos;

public sealed record AppointmentReceiptDto(string ReferenceCode, DateOnly AppointmentDate, string TimeSlot, AppointmentStatus Status, bool IsNew);
public sealed record AppointmentAdminDto(int Id, string ReferenceCode, string FullName, string Phone, string Email,
    string ServiceTitle, DateOnly AppointmentDate, string TimeSlot, int GuestCount, string? Notes,
    AppointmentStatus Status, DateTime CreatedDate, string? GiftCode = null);
