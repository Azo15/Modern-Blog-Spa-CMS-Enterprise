using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using FluentValidation;
using Microsoft.Extensions.Localization;
namespace BusinessLayer.ValidationRules;
public sealed class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>
{
    public CreateCommentCommandValidator(IStringLocalizer<SharedResource>? localizer = null)
    {
        RuleFor(x => x.BlogId).GreaterThan(0).Localized(localizer, "PositiveId");
        RuleFor(x => x.Content).NotEmpty().Localized(localizer, "Required").MaximumLength(1000).Localized(localizer, "MaxLength", 1000);
        RuleFor(x => x.ParentCommentId).GreaterThan(0).Localized(localizer, "PositiveId").When(x => x.ParentCommentId.HasValue);
    }
}
public sealed class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>
{
    public UpdateCommentCommandValidator(IStringLocalizer<SharedResource>? localizer = null)
    {
        RuleFor(x => x.Id).GreaterThan(0).Localized(localizer, "PositiveId");
        RuleFor(x => x.Content).NotEmpty().Localized(localizer, "Required").MaximumLength(1000).Localized(localizer, "MaxLength", 1000);
    }
}
public sealed class ApproveCommentCommandValidator : AbstractValidator<ApproveCommentCommand>
{
    public ApproveCommentCommandValidator(IStringLocalizer<SharedResource>? localizer = null)
        => RuleFor(x => x.Id).GreaterThan(0).Localized(localizer, "PositiveId");
}
public sealed class DeleteCommentCommandValidator : AbstractValidator<DeleteCommentCommand>
{
    public DeleteCommentCommandValidator(IStringLocalizer<SharedResource>? localizer = null)
        => RuleFor(x => x.Id).GreaterThan(0).Localized(localizer, "PositiveId");
}
public sealed class GetPublicCommentsQueryValidator : AbstractValidator<GetPublicCommentsQuery>
{
    public GetPublicCommentsQueryValidator(IStringLocalizer<SharedResource>? localizer = null)
    {
        RuleFor(x => x.BlogId).GreaterThan(0).Localized(localizer, "PositiveId");
        RuleFor(x => x.PageIndex).InclusiveBetween(1, 1000000).Localized(localizer, "InvalidPage");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50).Localized(localizer, "InvalidPageSize", 50);
    }
}
public sealed class GetPendingCommentsQueryValidator : AbstractValidator<GetPendingCommentsQuery>
{
    public GetPendingCommentsQueryValidator(IStringLocalizer<SharedResource>? localizer = null)
    {
        RuleFor(x => x.PageIndex).InclusiveBetween(1, 1000000).Localized(localizer, "InvalidPage");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).Localized(localizer, "InvalidPageSize", 100);
    }
}
public sealed class GetMyCommentsQueryValidator : AbstractValidator<GetMyCommentsQuery>
{
    public GetMyCommentsQueryValidator(IStringLocalizer<SharedResource>? localizer = null)
        => RuleFor(x => x.BlogId).GreaterThan(0).Localized(localizer, "PositiveId");
}
