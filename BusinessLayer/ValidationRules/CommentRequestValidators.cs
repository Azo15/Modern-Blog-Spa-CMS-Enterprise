using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using FluentValidation;
namespace BusinessLayer.ValidationRules;
public sealed class CreateCommentCommandValidator : AbstractValidator<CreateCommentCommand>{public CreateCommentCommandValidator(){RuleFor(x=>x.BlogId).GreaterThan(0);RuleFor(x=>x.Content).NotEmpty().MaximumLength(1000);RuleFor(x=>x.ParentCommentId).GreaterThan(0).When(x=>x.ParentCommentId.HasValue);}}
public sealed class UpdateCommentCommandValidator : AbstractValidator<UpdateCommentCommand>{public UpdateCommentCommandValidator(){RuleFor(x=>x.Id).GreaterThan(0);RuleFor(x=>x.Content).NotEmpty().MaximumLength(1000);}}
public sealed class ApproveCommentCommandValidator : AbstractValidator<ApproveCommentCommand>{public ApproveCommentCommandValidator(){RuleFor(x=>x.Id).GreaterThan(0);}}
public sealed class DeleteCommentCommandValidator : AbstractValidator<DeleteCommentCommand>{public DeleteCommentCommandValidator(){RuleFor(x=>x.Id).GreaterThan(0);}}
public sealed class GetPublicCommentsQueryValidator : AbstractValidator<GetPublicCommentsQuery>{public GetPublicCommentsQueryValidator(){RuleFor(x=>x.BlogId).GreaterThan(0);RuleFor(x=>x.PageIndex).InclusiveBetween(1,1000000);RuleFor(x=>x.PageSize).InclusiveBetween(1,50);}}
public sealed class GetPendingCommentsQueryValidator : AbstractValidator<GetPendingCommentsQuery>{public GetPendingCommentsQueryValidator(){RuleFor(x=>x.PageIndex).InclusiveBetween(1,1000000);RuleFor(x=>x.PageSize).InclusiveBetween(1,100);}}
public sealed class GetMyCommentsQueryValidator : AbstractValidator<GetMyCommentsQuery>{public GetMyCommentsQueryValidator(){RuleFor(x=>x.BlogId).GreaterThan(0);}}
