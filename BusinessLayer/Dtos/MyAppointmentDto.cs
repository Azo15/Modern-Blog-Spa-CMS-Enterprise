using EntityLayer.Enum;
using BusinessLayer.ValidationRules;
namespace BusinessLayer.Dtos;
public sealed record MyAppointmentDto(int Id, string ReferenceCode, int ServiceId, string ServiceTitle,
    DateOnly AppointmentDate, string TimeSlot, int GuestCount, string? Notes,
    AppointmentStatus Status, DateTime? UpdatedDate)
{
    public bool CanChange => Status != AppointmentStatus.Cancelled && AppointmentSchedule.IsFutureSlot(AppointmentDate, TimeSlot);
}
