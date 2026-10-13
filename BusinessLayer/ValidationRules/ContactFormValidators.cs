using EntityLayer.Concrete;
using FluentValidation;

namespace BusinessLayer.ValidationRules;

public sealed class ContactFormValidators : AbstractValidator<ContactForm>
{
    public ContactFormValidators()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Phone).MaximumLength(32);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(5000);
    }
}
