using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using EntityLayer.Enum;
using FluentValidation;

namespace BusinessLayer.ValidationRules;

public static class AppointmentSchedule
{
    public static readonly IReadOnlyList<string> TimeSlots = Array.AsReadOnly(new[] { "10:00", "11:30", "14:00", "16:30", "18:00", "20:00", "22:00" });
    public static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Turkey Standard Time" : "Europe/Istanbul");
    public static DateTime LocalNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Istanbul);
    public static bool IsFutureSlot(DateOnly date, string? slot)
    {
        if (!TimeSlots.Contains(slot ?? string.Empty)) return false;
        return date.ToDateTime(TimeOnly.ParseExact(slot!, "HH:mm", System.Globalization.CultureInfo.InvariantCulture)) > LocalNow;
    }
}

public sealed class CreateAppointmentValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.ServiceId).GreaterThan(0).WithMessage("Lütfen bir hizmet seçin.");
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120).Must(name => name?.Trim().Length is >= 2 and <= 120).WithMessage("Ad soyad 2–120 karakter olmalıdır.");
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(30)
            .Must(phone => phone is not null && System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+?[0-9 ()-]+$") && phone.Count(char.IsDigit) is >= 10 and <= 15)
            .WithMessage("Geçerli bir telefon numarası girin.");
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.");
        RuleFor(x => x.GuestCount).InclusiveBetween(1, 4).WithMessage("Misafir sayısı 1–4 arasında olmalıdır.");
        RuleFor(x => x.Notes).MaximumLength(2000).WithMessage("Notunuz en fazla 2000 karakter olabilir.");
        RuleFor(x => x.TimeSlot).Must(slot => AppointmentSchedule.TimeSlots.Contains(slot ?? string.Empty)).WithMessage("Lütfen geçerli bir saat dilimi seçin.");
        RuleFor(x => x.AppointmentDate).Must(date => date >= DateOnly.FromDateTime(AppointmentSchedule.LocalNow) && date <= DateOnly.FromDateTime(AppointmentSchedule.LocalNow).AddDays(90))
            .WithMessage("Randevu tarihi bugün ile önümüzdeki 90 gün arasında olmalıdır.");
        RuleFor(x => x.AppointmentDate).Must((request, date) => AppointmentSchedule.IsFutureSlot(date, request.TimeSlot))
            .WithMessage("Lütfen henüz geçmemiş bir gün ve saat seçin.");
        RuleFor(x => x.GiftCode).MaximumLength(40).Matches(@"^[A-Za-z0-9][A-Za-z0-9-]{2,39}$")
            .When(x => !string.IsNullOrEmpty(x.GiftCode)).WithMessage("Kod 3–40 karakter olmalı; yalnızca harf, rakam ve tire kullanın.");
        RuleFor(x => x.ClientRequestId).NotEmpty().WithMessage("Talep kimliği eksik. Sayfayı yenileyip tekrar deneyin.");
    }
}

public sealed class GetAppointmentsValidator : AbstractValidator<GetAppointmentsQuery>
{
    public GetAppointmentsValidator()
    {
        RuleFor(x => x.PageIndex).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).Must(status => status is null || Enum.IsDefined(status.Value));
    }
}

public sealed class ChangeAppointmentStatusValidator : AbstractValidator<ChangeAppointmentStatusCommand>
{
    public ChangeAppointmentStatusValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Status).Must(status => status is AppointmentStatus.Confirmed or AppointmentStatus.Cancelled);
    }
}
