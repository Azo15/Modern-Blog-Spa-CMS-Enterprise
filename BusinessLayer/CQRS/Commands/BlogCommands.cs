using EntityLayer.Enum;
using MediatR;
using Microsoft.AspNetCore.Http;
namespace BusinessLayer.CQRS.Commands;
public record CreateBlogCommand(string Title, string SubTitle, string Content, int CatagoryId, IFormFile? ImageFile = null) : IRequest<int>;
public record UpdateBlogCommand(int Id, string Title, string SubTitle, string Content, int CatagoryId, IFormFile? ImageFile = null) : IRequest<bool>;
public record DeleteBlogCommand(int Id) : IRequest<bool>;
public record ChangeBlogStatusCommand(int Id, PublishStatus Status) : IRequest<bool>;
public record RestoreBlogCommand(int Id) : IRequest<bool>;
