using System.ComponentModel.DataAnnotations;
using BusinessLayer.Dtos;

namespace WebUI.Api;

/// <summary>The content accepted when creating or replacing an author's blog.</summary>
public sealed record BlogWriteRequest(
    [property: Required, StringLength(150, MinimumLength = 5)] string Title,
    [property: Required, StringLength(2000, MinimumLength = 10)] string SubTitle,
    [property: Required, StringLength(100000)] string Content,
    [property: Range(1, int.MaxValue)] int CategoryId);

/// <summary>A comment or one direct reply to an approved root comment.</summary>
public sealed record CommentWriteRequest(
    [property: Range(1, int.MaxValue)] int BlogId,
    [property: Required, StringLength(1000)] string Content,
    [property: Range(1, int.MaxValue)] int? ParentCommentId = null);

/// <summary>Identity credentials for API clients. This endpoint does not issue a cookie.</summary>
public sealed record ApiLoginRequest(
    [property: Required, EmailAddress, StringLength(256)] string Email,
    [property: Required, StringLength(1024)] string Password)
{
    public override string ToString() => "ApiLoginRequest { Credentials = [REDACTED] }";
}

/// <summary>The identifier returned after a command creates a resource.</summary>
public sealed record CreatedResourceDto(int Id);

/// <summary>The result of updating or deleting an existing resource.</summary>
public sealed record ResourceChangedDto(int Id);

/// <summary>A published blog and a page of approved root comments with approved direct replies.</summary>
public sealed record PublicBlogPostDto(BlogDetailDto Blog, PaginatedList<CommentThreadDto> Comments);
