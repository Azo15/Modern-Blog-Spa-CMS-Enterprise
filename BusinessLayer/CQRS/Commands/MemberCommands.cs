using BusinessLayer.Dtos;
using EntityLayer.Enum;
using MediatR;

namespace BusinessLayer.CQRS.Commands;

public sealed record ToggleBlogFavoriteCommand(int BlogId, bool RemoveOnly = false) : IRequest<BlogInteractionDto>;
// Repeating the current reaction withdraws it; null explicitly withdraws any reaction.
public sealed record SetBlogReactionCommand(int BlogId, ReactionType? ReactionType) : IRequest<BlogInteractionDto>;
public sealed record SetAccountLockoutCommand(int UserId, bool IsLocked, int Days = 30) : IRequest<bool>;
