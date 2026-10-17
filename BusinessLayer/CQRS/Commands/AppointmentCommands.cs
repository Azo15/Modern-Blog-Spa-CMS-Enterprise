using BusinessLayer.Dtos;
using EntityLayer.Enum;
using MediatR;

namespace BusinessLayer.CQRS.Commands;

public sealed record CreateAppointmentCommand(int ServiceId, DateOnly AppointmentDate, string TimeSlot,
    int GuestCount, string FullName, string Phone, string Email, string? Notes, Guid ClientRequestId, string? GiftCode = null) : IRequest<AppointmentReceiptDto>;
public sealed record ChangeAppointmentStatusCommand(int Id, AppointmentStatus Status) : IRequest<bool>;
