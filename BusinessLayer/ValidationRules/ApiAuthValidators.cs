using BusinessLayer.CQRS.Commands;
using FluentValidation;

namespace BusinessLayer.ValidationRules;

public sealed class ApiLoginCommandValidator : AbstractValidator<ApiLoginCommand>
{
    public ApiLoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(1024);
    }
}
