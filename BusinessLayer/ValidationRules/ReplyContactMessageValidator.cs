using BusinessLayer.CQRS.Commands;
using FluentValidation;
namespace BusinessLayer.ValidationRules;
public sealed class ReplyContactMessageValidator : AbstractValidator<ReplyContactMessageCommand>
{
    public ReplyContactMessageValidator()
    {
        RuleFor(x => x.ContactFormId).GreaterThan(0);
        RuleFor(x => x.ReplyMessage).NotEmpty().MaximumLength(5000).WithMessage("Yanıt 1–5000 karakter olmalıdır.");
        RuleFor(x => x.ClientRequestId).NotEqual(Guid.Empty);
    }
}
