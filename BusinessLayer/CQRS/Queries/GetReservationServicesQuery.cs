using BusinessLayer.Dtos;
using MediatR;

namespace BusinessLayer.CQRS.Queries;

public sealed record GetReservationServicesQuery : IRequest<IReadOnlyList<ReservationServiceDto>>;
