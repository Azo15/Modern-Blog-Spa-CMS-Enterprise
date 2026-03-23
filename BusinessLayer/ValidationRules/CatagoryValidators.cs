using BusinessLayer.Dtos;
using FluentValidation;

namespace BusinessLayer.ValidationRules
{
    public class CatagoryCreateValidator : AbstractValidator<CatagoryCreateDto>
    {
        public CatagoryCreateValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Kategori adı boş geçilemez")
                .MinimumLength(2).WithMessage("Kategori adı en az 2 karakter olmalıdır");
        }
    }

    public class CatagoryUpdateValidator : AbstractValidator<CatagoryUpdateDto>
    {
        public CatagoryUpdateValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().WithMessage("Kategori adı boş geçilemez");
        }
    }
}
