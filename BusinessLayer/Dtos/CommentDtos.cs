namespace BusinessLayer.Dtos;
public sealed record CommentDto(int Id, int? ParentCommentId, string Content, string? AuthorName, DateTime CreatedDate, bool CanEdit);
public sealed record CommentThreadDto(CommentDto Comment, IReadOnlyList<CommentDto> Children);
public sealed record PendingCommentDto(int Id, int BlogId, int? ParentCommentId, string Content, string? AuthorName, DateTime CreatedDate);
public sealed record OwnCommentDto(int Id, string Content, bool IsApproved);
