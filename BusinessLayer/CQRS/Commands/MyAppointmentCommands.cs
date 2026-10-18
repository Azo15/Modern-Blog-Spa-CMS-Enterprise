using MediatR;
namespace BusinessLayer.CQRS.Commands;
public sealed record UpdateMyAppointmentCommand(int Id, int ServiceId, DateOnly AppointmentDate,
    string TimeSlot, int GuestCount, string? Notes, DateTime? ExpectedUpdatedDate) : IRequest<bool>;
public sealed record CancelMyAppointmentCommand(int Id, DateTime? ExpectedUpdatedDate) : IRequest<bool>;
