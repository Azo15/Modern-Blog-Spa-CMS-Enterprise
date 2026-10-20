using EntityLayer.Enum;

namespace BusinessLayer.Dtos;

public sealed record BlogInteractionDto(bool IsFavorite, ReactionType? Reaction, int LikeCount, int HeartCount, int ClapCount, int FavoriteCount = 0);
public sealed record FavoriteBlogDto(int BlogId, string? Title, string? Slug, string? Image, DateTime FavoritedDate);
public sealed record ModerationAccountDto(int Id, string? FullName, string? Email, bool IsLockedOut, DateTimeOffset? LockoutEnd, bool CanManage);
