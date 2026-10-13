using BusinessLayer.Dtos;
using MediatR;
namespace BusinessLayer.CQRS.Queries;
public record GetPublicCommentsQuery(int BlogId, int PageIndex = 1, int PageSize = 10) : IRequest<PaginatedList<CommentThreadDto>>;
public record GetPendingCommentsQuery(int PageIndex = 1, int PageSize = 20) : IRequest<PaginatedList<PendingCommentDto>>;
public record GetMyCommentsQuery(int BlogId) : IRequest<List<OwnCommentDto>>;
