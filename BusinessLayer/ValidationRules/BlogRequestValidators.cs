using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using FluentValidation;
using Microsoft.Extensions.Localization;
namespace BusinessLayer.ValidationRules;
// Resolve messages when validation runs, so a validator follows the request culture.
internal static class ValidationLocalization
{
    public static IRuleBuilderOptions<T, TProperty> Localized<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule, IStringLocalizer<SharedResource>? localizer,
        string key, params object[] arguments)
        => localizer is null ? rule : rule.WithMessage(_ => localizer[key, arguments].Value);
}
public sealed class CreateBlogCommandValidator : AbstractValidator<CreateBlogCommand>
{
    public CreateBlogCommandValidator(IStringLocalizer<SharedResource>? localizer = null)
    {
        RuleFor(x => x.Title).NotEmpty().Localized(localizer, "Required").Length(5, 150).Localized(localizer, "LengthBetween", 5, 150);
        RuleFor(x => x.SubTitle).NotEmpty().Localized(localizer, "Required").MinimumLength(10).Localized(localizer, "MinLength", 10);
        RuleFor(x => x.Content).NotEmpty().Localized(localizer, "Required").MaximumLength(100000).Localized(localizer, "MaxLength", 100000);
        RuleFor(x => x.CatagoryId).GreaterThan(0).Localized(localizer, "PositiveId");
    }
}
public sealed class UpdateBlogCommandValidator : AbstractValidator<UpdateBlogCommand>
{
    public UpdateBlogCommandValidator(IStringLocalizer<SharedResource>? localizer = null)
    {
        RuleFor(x => x.Id).GreaterThan(0).Localized(localizer, "PositiveId");
        RuleFor(x => x.Title).NotEmpty().Localized(localizer, "Required").Length(5, 150).Localized(localizer, "LengthBetween", 5, 150);
        RuleFor(x => x.SubTitle).NotEmpty().Localized(localizer, "Required").MinimumLength(10).Localized(localizer, "MinLength", 10);
        RuleFor(x => x.Content).NotEmpty().Localized(localizer, "Required").MaximumLength(100000).Localized(localizer, "MaxLength", 100000);
        RuleFor(x => x.CatagoryId).GreaterThan(0).Localized(localizer, "PositiveId");
    }
}
public sealed class DeleteBlogCommandValidator : AbstractValidator<DeleteBlogCommand>
{
    public DeleteBlogCommandValidator(IStringLocalizer<SharedResource>? localizer = null)
        => RuleFor(x => x.Id).GreaterThan(0).Localized(localizer, "PositiveId");
}
public sealed class RestoreBlogCommandValidator : AbstractValidator<RestoreBlogCommand>
{
    public RestoreBlogCommandValidator(IStringLocalizer<SharedResource>? localizer = null)
        => RuleFor(x => x.Id).GreaterThan(0).Localized(localizer, "PositiveId");
}
public sealed class ChangeBlogStatusCommandValidator : AbstractValidator<ChangeBlogStatusCommand>
{
    public ChangeBlogStatusCommandValidator(IStringLocalizer<SharedResource>? localizer = null)
    {
        RuleFor(x => x.Id).GreaterThan(0).Localized(localizer, "PositiveId");
        RuleFor(x => x.Status).IsInEnum().Localized(localizer, "InvalidStatus");
    }
}
public sealed class GetBlogsQueryValidator : AbstractValidator<GetBlogsQuery>
{
    public GetBlogsQueryValidator(IStringLocalizer<SharedResource>? localizer = null)
    {
        RuleFor(x => x.PageIndex).InclusiveBetween(1, 1000000).Localized(localizer, "InvalidPage");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).Localized(localizer, "InvalidPageSize", 100);
        RuleFor(x => x.CategoryId).GreaterThan(0).Localized(localizer, "PositiveId").When(x => x.CategoryId.HasValue);
    }
}
public sealed class GetPublicBlogsQueryValidator : AbstractValidator<GetPublicBlogsQuery>
{
    public GetPublicBlogsQueryValidator(IStringLocalizer<SharedResource>? localizer = null)
    {
        RuleFor(x => x.PageIndex).InclusiveBetween(1, 1000000).Localized(localizer, "InvalidPage");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).Localized(localizer, "InvalidPageSize", 100);
        RuleFor(x => x.CategoryId).GreaterThan(0).Localized(localizer, "PositiveId").When(x => x.CategoryId.HasValue);
        RuleFor(x => x.ExcludeId).GreaterThan(0).Localized(localizer, "PositiveId").When(x => x.ExcludeId.HasValue);
        RuleFor(x => x.Search).MaximumLength(100).Localized(localizer, "MaxLength", 100);
    }
}
public sealed class GetTrashedBlogsQueryValidator : AbstractValidator<GetTrashedBlogsQuery>
{
    public GetTrashedBlogsQueryValidator(IStringLocalizer<SharedResource>? localizer = null)
    {
        RuleFor(x => x.PageIndex).InclusiveBetween(1, 1000000).Localized(localizer, "InvalidPage");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).Localized(localizer, "InvalidPageSize", 100);
    }
}
public sealed class GetBlogByIdQueryValidator : AbstractValidator<GetBlogByIdQuery>
{
    public GetBlogByIdQueryValidator(IStringLocalizer<SharedResource>? localizer = null)
        => RuleFor(x => x.Id).GreaterThan(0).Localized(localizer, "PositiveId");
}
public sealed class GetPublicBlogBySlugQueryValidator : AbstractValidator<GetPublicBlogBySlugQuery>
{
    public GetPublicBlogBySlugQueryValidator(IStringLocalizer<SharedResource>? localizer = null)
        => RuleFor(x => x.Slug).NotEmpty().Localized(localizer, "Required").MaximumLength(250).Localized(localizer, "MaxLength", 250);
}
