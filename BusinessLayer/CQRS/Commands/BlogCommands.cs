using MediatR;

namespace BusinessLayer.CQRS.Commands
{
    public record CreateBlogCommand(
        string Title,
        string SubTitle,
        string Content,
        string? Image,
        int CatagoryId,
        int? AppUserId
    ) : IRequest<int>;

    public record UpdateBlogCommand(
        int Id,
        string Title,
        string SubTitle,
        string Content,
        string? Image,
        int CatagoryId
    ) : IRequest<bool>;

    public record DeleteBlogCommand(int Id) : IRequest<bool>;
}
