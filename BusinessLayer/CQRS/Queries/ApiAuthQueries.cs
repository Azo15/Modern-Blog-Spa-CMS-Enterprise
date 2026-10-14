using BusinessLayer.Dtos;
using MediatR;

namespace BusinessLayer.CQRS.Queries;

/// <summary>Returns the active API account's safe identity projection.</summary>
public sealed record GetApiIdentityQuery : IRequest<ApiIdentityDto>;
