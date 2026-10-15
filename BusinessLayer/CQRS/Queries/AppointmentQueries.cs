using BusinessLayer.Dtos;
using EntityLayer.Enum;
using MediatR;

namespace BusinessLayer.CQRS.Queries;

public sealed record GetAppointmentsQuery(AppointmentStatus? Status = null, int PageIndex = 1, int PageSize = 20) : IRequest<PaginatedList<AppointmentAdminDto>>;
