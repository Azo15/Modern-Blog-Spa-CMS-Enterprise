using EntityLayer.Enum;

namespace BusinessLayer.Dtos;

// Public DTOs deliberately contain no Identity entity, email, hash or security stamp.
public sealed record BlogListDto(int Id, string? Title, string? SubTitle, string? Slug, string? Image,
    int? CatagoryId, string? CategoryTitle, string? AuthorName, DateTime CreatedDate);
public sealed record BlogDetailDto(int Id, string? Title, string? SubTitle, string? Slug, string? Image,
    int? CatagoryId, string? CategoryTitle, string? AuthorName, DateTime CreatedDate, string? Content);
public sealed record BlogAdminDto(int Id, string? Title, string? SubTitle, string? Slug, string? Image,
    int? CatagoryId, int? AppUserId, string? Content, PublishStatus PublishStatus,
    DateTime CreatedDate, DateTime? UpdatedDate, DateTime? DeletedDate, string? DeletedBy);
public sealed record CategoryOptionDto(int Id, string? Title);
