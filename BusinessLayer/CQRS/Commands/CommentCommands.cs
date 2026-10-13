using MediatR;
namespace BusinessLayer.CQRS.Commands;
public record CreateCommentCommand(int BlogId, string Content, int? ParentCommentId = null) : IRequest<int>;
public record UpdateCommentCommand(int Id, string Content) : IRequest<bool>;
public record DeleteCommentCommand(int Id) : IRequest<bool>;
public record ApproveCommentCommand(int Id) : IRequest<bool>;
