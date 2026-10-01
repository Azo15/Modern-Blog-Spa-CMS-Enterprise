using EntityLayer.Concrete;
using MediatR;

namespace BusinessLayer.CQRS.Queries
{
    public record GetAllBlogsQuery(int? CategoryId = null) : IRequest<List<Blog>>;

    public record GetBlogByIdQuery(int Id) : IRequest<Blog?>;
}
