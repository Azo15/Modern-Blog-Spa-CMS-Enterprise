using MediatR;
using BusinessLayer.Dtos;
namespace BusinessLayer.CQRS.Queries;
public sealed record GetAdminDashboardQuery : IRequest<AdminDashboardDto>;
