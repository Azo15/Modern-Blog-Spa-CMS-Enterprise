using BusinessLayer.Dtos;
using FluentValidation;

namespace BusinessLayer.ValidationRules
{
    public class BlogCreateValidator : AbstractValidator<BlogCreateDto>
    {
        public BlogCreateValidator()
        {
            RuleFor(x => x.Title).NotEmpty().WithMessage("Blog başlığı boş geçilemez")
                .MinimumLength(5).WithMessage("Blog başlığı en az 5 karakter olmalıdır")
                .MaximumLength(150).WithMessage("Blog başlığı en fazla 150 karakter olmalıdır");

            RuleFor(x => x.SubTitle).NotEmpty().WithMessage("Blog alt başlığı boş geçilemez")
                .MinimumLength(10).WithMessage("Blog alt başlığı en az 10 karakter olmalıdır");

            RuleFor(x => x.Content).NotEmpty().WithMessage("Blog içeriği boş geçilemez");

            RuleFor(x => x.CatagoryId).NotEmpty().WithMessage("Lütfen bir kategori seçiniz");
        }
    }

    public class BlogUpdateValidator : AbstractValidator<BlogUpdateDto>
    {
        public BlogUpdateValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().WithMessage("Blog başlığı boş geçilemez")
                .MinimumLength(5).WithMessage("Blog başlığı en az 5 karakter olmalıdır");
            RuleFor(x => x.SubTitle).NotEmpty().WithMessage("Blog alt başlığı boş geçilemez");
            RuleFor(x => x.Content).NotEmpty().WithMessage("Blog içeriği boş geçilemez");
            RuleFor(x => x.CatagoryId).NotEmpty().WithMessage("Lütfen bir kategori seçiniz");
        }
    }
}
