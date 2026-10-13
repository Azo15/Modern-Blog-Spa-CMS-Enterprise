using BusinessLayer.Dtos;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace BusinessLayer.ValidationRules
{
    public class BlogCreateValidator : AbstractValidator<BlogCreateDto>
    {
        public BlogCreateValidator(IStringLocalizer<SharedResource>? localizer = null)
        {
            RuleFor(x => x.Title).NotEmpty().Localized(localizer, "Required")
                .MinimumLength(5).Localized(localizer, "MinLength", 5)
                .MaximumLength(150).Localized(localizer, "MaxLength", 150);

            RuleFor(x => x.SubTitle).NotEmpty().Localized(localizer, "Required")
                .MinimumLength(10).Localized(localizer, "MinLength", 10);

            RuleFor(x => x.Content).NotEmpty().Localized(localizer, "Required");

            RuleFor(x => x.CatagoryId).NotEmpty().Localized(localizer, "PositiveId");
        }
    }

    public class BlogUpdateValidator : AbstractValidator<BlogUpdateDto>
    {
        public BlogUpdateValidator(IStringLocalizer<SharedResource>? localizer = null)
        {
            RuleFor(x => x.Id).NotEmpty().Localized(localizer, "PositiveId");
            RuleFor(x => x.Title).NotEmpty().Localized(localizer, "Required")
                .MinimumLength(5).Localized(localizer, "MinLength", 5);
            RuleFor(x => x.SubTitle).NotEmpty().Localized(localizer, "Required");
            RuleFor(x => x.Content).NotEmpty().Localized(localizer, "Required");
            RuleFor(x => x.CatagoryId).NotEmpty().Localized(localizer, "PositiveId");
        }
    }
}
