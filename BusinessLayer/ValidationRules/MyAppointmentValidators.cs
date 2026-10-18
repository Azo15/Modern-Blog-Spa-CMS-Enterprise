using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using FluentValidation;
namespace BusinessLayer.ValidationRules;
public sealed class UpdateMyAppointmentValidator : AbstractValidator<UpdateMyAppointmentCommand>
{
    public UpdateMyAppointmentValidator()
    {
        RuleFor(r => r.Id).GreaterThan(0);
        RuleFor(r => r.ServiceId).GreaterThan(0);
        RuleFor(r => r.GuestCount).InclusiveBetween(1, 4);
        RuleFor(r => r.Notes).MaximumLength(2000);
        RuleFor(r => r.AppointmentDate).Must((r,d) => AppointmentSchedule.IsFutureSlot(d,r.TimeSlot) &&
            d <= DateOnly.FromDateTime(AppointmentSchedule.LocalNow).AddDays(90))
            .WithMessage("Önümüzdeki 90 gün içinde geçerli bir gün ve saat seçin.");
    }
}
public sealed class GetMyAppointmentsValidator : AbstractValidator<GetMyAppointmentsQuery>
{
    public GetMyAppointmentsValidator() => RuleFor(r => r.PageIndex).InclusiveBetween(1, 100000);
}
