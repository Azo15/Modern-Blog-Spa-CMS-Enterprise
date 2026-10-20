using BusinessLayer.CQRS.Commands;
using FluentValidation;
namespace BusinessLayer.ValidationRules;
public sealed class GenerateAiBlogValidator : AbstractValidator<GenerateAiBlogCommand>
{
    public GenerateAiBlogValidator()
    {
        RuleFor(x=>x.ReadingMinutes).InclusiveBetween(2,8);
        RuleFor(x=>x.Topic).NotEmpty().Length(5,200);
        RuleFor(x=>x.CategoryId).GreaterThan(0);
        RuleFor(x=>x.Language).Must(x=>x is "tr-TR" or "en-US");
        RuleFor(x=>x.Tone).Must(x=>x is "luxury" or "wellness" or "guide");
    }
}
