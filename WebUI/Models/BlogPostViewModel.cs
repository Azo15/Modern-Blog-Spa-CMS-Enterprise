using BusinessLayer.Dtos;
namespace WebUI.Models;
public sealed record BlogPostViewModel(BlogDetailDto Blog,PaginatedList<CommentThreadDto> Comments,IReadOnlyList<OwnCommentDto> OwnComments);
