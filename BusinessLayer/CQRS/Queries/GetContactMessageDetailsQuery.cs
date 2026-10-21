using BusinessLayer.Dtos;
using MediatR;
namespace BusinessLayer.CQRS.Queries;
public sealed record GetContactMessageDetailsQuery(int Id, int Page = 1) : IRequest<ContactMessageDetailsDto?>;
