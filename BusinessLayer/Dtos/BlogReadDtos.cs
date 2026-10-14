using EntityLayer.Enum;

namespace BusinessLayer.Dtos;

// Public DTOs deliberately contain no Identity entity, email, hash or security stamp.
public sealed record BlogListDto(int Id, string? Title, string? SubTitle, string? Slug, string? Image,
    int? CatagoryId, string? CategoryTitle, string? AuthorName, DateTime CreatedDate,
    int ReadingMinutes = 0, int LikeCount = 0, int HeartCount = 0);
public sealed record BlogDetailDto(int Id, string? Title, string? SubTitle, string? Slug, string? Image,
    int? CatagoryId, string? CategoryTitle, string? AuthorName, DateTime CreatedDate, string? Content);
public sealed record BlogAdminDto(int Id, string? Title, string? SubTitle, string? Slug, string? Image,
    int? CatagoryId, int? AppUserId, string? Content, PublishStatus PublishStatus,
    DateTime CreatedDate, DateTime? UpdatedDate, DateTime? DeletedDate, string? DeletedBy);
public sealed record CategoryOptionDto(int Id, string? Title);
/// <summary>A public category and the number of currently published, visible blogs it contains.</summary>
public sealed record PublicCategoryDto(int Id, string? Title, int BlogCount);
