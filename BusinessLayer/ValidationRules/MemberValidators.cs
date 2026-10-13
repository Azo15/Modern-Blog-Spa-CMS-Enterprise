using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace BusinessLayer.ValidationRules;

public sealed class ToggleBlogFavoriteValidator : AbstractValidator<ToggleBlogFavoriteCommand>
{ public ToggleBlogFavoriteValidator(IStringLocalizer<SharedResource> text) => RuleFor(x => x.BlogId).GreaterThan(0).WithMessage(_ => text["PositiveId"]); }
public sealed class SetBlogReactionValidator : AbstractValidator<SetBlogReactionCommand>
{
    public SetBlogReactionValidator(IStringLocalizer<SharedResource> text)
    {
        RuleFor(x => x.BlogId).GreaterThan(0).WithMessage(_ => text["PositiveId"]);
        RuleFor(x => x.ReactionType).Must(type => type is null || System.Enum.IsDefined(type.Value)).WithMessage(_ => text["InvalidReaction"]);
    }
}
public sealed class SetAccountLockoutValidator : AbstractValidator<SetAccountLockoutCommand>
{
    public SetAccountLockoutValidator(IStringLocalizer<SharedResource> text)
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage(_ => text["PositiveId"]);
        RuleFor(x => x.Days).InclusiveBetween(1, 3650).When(x => x.IsLocked).WithMessage(_ => text["LockDaysRange"]);
    }
}
public sealed class GetUserFavoritesValidator : AbstractValidator<GetUserFavoritesQuery>
{ public GetUserFavoritesValidator(IStringLocalizer<SharedResource> text) { RuleFor(x => x.PageIndex).GreaterThan(0).WithMessage(_ => text["InvalidPage"]); RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage(_ => text["InvalidPageSize", 100]); } }
public sealed class GetModerationAccountsValidator : AbstractValidator<GetModerationAccountsQuery>
{ public GetModerationAccountsValidator(IStringLocalizer<SharedResource> text) { RuleFor(x => x.PageIndex).GreaterThan(0).WithMessage(_ => text["InvalidPage"]); RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage(_ => text["InvalidPageSize", 100]); RuleFor(x => x.Search).MaximumLength(200).WithMessage(_ => text["MaxLength", 200]); } }
