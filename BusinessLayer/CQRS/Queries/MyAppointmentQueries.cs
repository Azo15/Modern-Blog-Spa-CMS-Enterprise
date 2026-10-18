using BusinessLayer.Dtos;
using MediatR;
namespace BusinessLayer.CQRS.Queries;
public sealed record GetMyAppointmentsQuery(int PageIndex = 1) : IRequest<PaginatedList<MyAppointmentDto>>;
public sealed record GetMyAppointmentQuery(int Id) : IRequest<MyAppointmentDto?>;
