using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using FluentValidation;

namespace BusinessLayer.ValidationRules;

public sealed class ToggleBlogFavoriteValidator : AbstractValidator<ToggleBlogFavoriteCommand>
{ public ToggleBlogFavoriteValidator() => RuleFor(x => x.BlogId).GreaterThan(0); }
public sealed class SetBlogReactionValidator : AbstractValidator<SetBlogReactionCommand>
{
    public SetBlogReactionValidator()
    {
        RuleFor(x => x.BlogId).GreaterThan(0);
        RuleFor(x => x.ReactionType).Must(type => type is null || System.Enum.IsDefined(type.Value)).WithMessage("Geçersiz tepki.");
    }
}
public sealed class SetAccountLockoutValidator : AbstractValidator<SetAccountLockoutCommand>
{
    public SetAccountLockoutValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Days).InclusiveBetween(1, 3650);
    }
}
public sealed class GetUserFavoritesValidator : AbstractValidator<GetUserFavoritesQuery>
{ public GetUserFavoritesValidator() { RuleFor(x => x.PageIndex).GreaterThan(0); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); } }
public sealed class GetModerationAccountsValidator : AbstractValidator<GetModerationAccountsQuery>
{ public GetModerationAccountsValidator() { RuleFor(x => x.PageIndex).GreaterThan(0); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); RuleFor(x => x.Search).MaximumLength(200); } }
