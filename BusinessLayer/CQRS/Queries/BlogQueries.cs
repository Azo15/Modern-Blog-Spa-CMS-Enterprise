using BusinessLayer.Dtos;
using MediatR;
namespace BusinessLayer.CQRS.Queries;
public record GetBlogsQuery(int PageIndex = 1, int PageSize = 20, int? CategoryId = null) : IRequest<PaginatedList<BlogAdminDto>>;
public record GetBlogByIdQuery(int Id) : IRequest<BlogAdminDto?>;
public record GetPublicBlogsQuery(int PageIndex = 1, int PageSize = 20, int? CategoryId = null, int? ExcludeId = null, string? Search = null) : IRequest<PaginatedList<BlogListDto>>;
public record GetPublicBlogBySlugQuery(string Slug) : IRequest<BlogDetailDto?>;
public record GetTrashedBlogsQuery(int PageIndex = 1, int PageSize = 20) : IRequest<PaginatedList<BlogAdminDto>>;
public record GetCategoryOptionsQuery : IRequest<List<CategoryOptionDto>>;
/// <summary>Anonymous category options with counts restricted to published, visible content.</summary>
public record GetPublicCategoriesQuery : IRequest<IReadOnlyList<PublicCategoryDto>>;
