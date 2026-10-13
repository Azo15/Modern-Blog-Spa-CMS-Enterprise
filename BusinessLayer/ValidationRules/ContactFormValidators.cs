using EntityLayer.Concrete;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace BusinessLayer.ValidationRules;

public sealed class ContactFormValidators : AbstractValidator<ContactForm>
{
    public ContactFormValidators(IStringLocalizer<SharedResource>? localizer = null)
    {
        RuleFor(x => x.FullName).NotEmpty().Localized(localizer, "Required").MaximumLength(100).Localized(localizer, "MaxLength", 100);
        RuleFor(x => x.Email).NotEmpty().Localized(localizer, "Required")
            .EmailAddress().Localized(localizer, "The {0} field is not a valid e-mail address.", localizer?["Email"].Value ?? "Email")
            .MaximumLength(254).Localized(localizer, "MaxLength", 254);
        RuleFor(x => x.Phone).MaximumLength(32).Localized(localizer, "MaxLength", 32);
        RuleFor(x => x.Subject).NotEmpty().Localized(localizer, "Required").MaximumLength(200).Localized(localizer, "MaxLength", 200);
        RuleFor(x => x.Message).NotEmpty().Localized(localizer, "Required").MaximumLength(5000).Localized(localizer, "MaxLength", 5000);
    }
}
