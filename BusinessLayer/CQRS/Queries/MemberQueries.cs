using BusinessLayer.Dtos;
using MediatR;

namespace BusinessLayer.CQRS.Queries;

public sealed record GetUserFavoritesQuery(int PageIndex = 1, int PageSize = 20) : IRequest<PaginatedList<FavoriteBlogDto>>;
public sealed record GetBlogInteractionQuery(int BlogId) : IRequest<BlogInteractionDto>;
public sealed record GetModerationAccountsQuery(int PageIndex = 1, int PageSize = 20, string? Search = null) : IRequest<PaginatedList<ModerationAccountDto>>;
