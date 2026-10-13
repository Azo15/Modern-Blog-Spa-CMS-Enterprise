using BusinessLayer.Dtos;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace BusinessLayer.ValidationRules
{
    public class CatagoryCreateValidator : AbstractValidator<CatagoryCreateDto>
    {
        public CatagoryCreateValidator(IStringLocalizer<SharedResource>? localizer = null)
        {
            RuleFor(x => x.Title).NotEmpty().Localized(localizer, "Required")
                .MinimumLength(2).Localized(localizer, "MinLength", 2);
        }
    }

    public class CatagoryUpdateValidator : AbstractValidator<CatagoryUpdateDto>
    {
        public CatagoryUpdateValidator(IStringLocalizer<SharedResource>? localizer = null)
        {
            RuleFor(x => x.Id).NotEmpty().Localized(localizer, "PositiveId");
            RuleFor(x => x.Title).NotEmpty().Localized(localizer, "Required");
        }
    }
}
