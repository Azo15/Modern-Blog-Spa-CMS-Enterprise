using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using FluentValidation;
namespace BusinessLayer.ValidationRules;
public sealed class CreateBlogCommandValidator : AbstractValidator<CreateBlogCommand>
{
    public CreateBlogCommandValidator() { RuleFor(x => x.Title).NotEmpty().Length(5, 150); RuleFor(x => x.SubTitle).NotEmpty().MinimumLength(10); RuleFor(x => x.Content).NotEmpty().MaximumLength(100000); RuleFor(x => x.CatagoryId).GreaterThan(0); }
}
public sealed class UpdateBlogCommandValidator : AbstractValidator<UpdateBlogCommand>
{
    public UpdateBlogCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Title).NotEmpty().Length(5, 150); RuleFor(x => x.SubTitle).NotEmpty().MinimumLength(10); RuleFor(x => x.Content).NotEmpty().MaximumLength(100000); RuleFor(x => x.CatagoryId).GreaterThan(0); }
}
public sealed class DeleteBlogCommandValidator : AbstractValidator<DeleteBlogCommand> { public DeleteBlogCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); } }
public sealed class RestoreBlogCommandValidator : AbstractValidator<RestoreBlogCommand> { public RestoreBlogCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); } }
public sealed class ChangeBlogStatusCommandValidator : AbstractValidator<ChangeBlogStatusCommand> { public ChangeBlogStatusCommandValidator() { RuleFor(x => x.Id).GreaterThan(0); RuleFor(x => x.Status).IsInEnum(); } }
public sealed class GetBlogsQueryValidator : AbstractValidator<GetBlogsQuery> { public GetBlogsQueryValidator() { RuleFor(x => x.PageIndex).InclusiveBetween(1, 1000000); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); RuleFor(x => x.CategoryId).GreaterThan(0).When(x => x.CategoryId.HasValue); } }
public sealed class GetPublicBlogsQueryValidator : AbstractValidator<GetPublicBlogsQuery> { public GetPublicBlogsQueryValidator() { RuleFor(x => x.PageIndex).InclusiveBetween(1, 1000000); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); RuleFor(x => x.CategoryId).GreaterThan(0).When(x => x.CategoryId.HasValue); } }
public sealed class GetTrashedBlogsQueryValidator : AbstractValidator<GetTrashedBlogsQuery> { public GetTrashedBlogsQueryValidator() { RuleFor(x => x.PageIndex).InclusiveBetween(1, 1000000); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); } }
public sealed class GetBlogByIdQueryValidator : AbstractValidator<GetBlogByIdQuery> { public GetBlogByIdQueryValidator() { RuleFor(x => x.Id).GreaterThan(0); } }
public sealed class GetPublicBlogBySlugQueryValidator : AbstractValidator<GetPublicBlogBySlugQuery> { public GetPublicBlogBySlugQueryValidator() { RuleFor(x => x.Slug).NotEmpty().MaximumLength(250); } }
