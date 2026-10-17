using System.Globalization;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using BusinessLayer.ValidationRules;
using DataAccesLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
namespace BusinessLayer.CQRS.Handlers;
public sealed class AdminDashboardHandler(AppDbContext db, ICurrentUserService current, IAuthorizationService authorization, IConfiguration configuration)
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken token)
    {
        if (!(await authorization.AuthorizeAsync(current.User, AccessPolicies.ContentManager)).Succeeded) throw new UnauthorizedAccessException();
        var today = DateOnly.FromDateTime(AppointmentSchedule.LocalNow); var yesterday = today.AddDays(-1);
        var month = new DateOnly(today.Year, today.Month, 1); var nextMonth = month.AddMonths(1); var start = today.AddDays(-13);
        var active = db.Appointments.AsNoTracking().Where(x => x.Status != AppointmentStatus.Cancelled);
        var todayCount = await active.CountAsync(x => x.AppointmentDate == today, token);
        var yesterdayCount = await active.CountAsync(x => x.AppointmentDate == yesterday, token);
        var pending = await db.Appointments.CountAsync(x => x.Status == AppointmentStatus.Pending, token);
        var customers = await active.Select(x => x.Email.ToUpper()).Distinct().CountAsync(token);
        var guests = await active.SumAsync(x => (int?)x.GuestCount, token) ?? 0;
        var services = await db.Services.CountAsync(x => x.IsPublish, token);
        var monthPrices = await active.Where(x => x.Status == AppointmentStatus.Confirmed && x.AppointmentDate >= month && x.AppointmentDate < nextMonth)
            .Select(x => x.Service.Content).ToListAsync(token);
        decimal revenue = 0;
        foreach (var price in monthPrices)
        {
            var part = (price ?? "").Split('—').Last().Replace("₺", "").Trim();
            if (decimal.TryParse(part, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out var amount)) revenue += amount;
        }
        var dates = await active.Where(x => x.AppointmentDate >= start && x.AppointmentDate <= today)
            .GroupBy(x => x.AppointmentDate).Select(x => new { Date = x.Key, Count = x.Count() }).ToListAsync(token);
        var trend = Enumerable.Range(0,14).Select(i => { var date = start.AddDays(i); return new DashboardDay(date.ToString("dd.MM", CultureInfo.InvariantCulture), dates.FirstOrDefault(x => x.Date == date)?.Count ?? 0); }).ToList();
        var distribution = await active.GroupBy(x => x.ServiceTitleSnapshot).Select(x => new DashboardDistribution(x.Key, x.Count())).OrderByDescending(x => x.Count).Take(8).ToListAsync(token);
        var recent = await db.Appointments.AsNoTracking().OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id).Take(8)
            .Select(x => new AppointmentAdminDto(x.Id,x.ReferenceCode,x.FullName,x.Phone,x.Email,x.ServiceTitleSnapshot,x.AppointmentDate,x.TimeSlot,x.GuestCount,x.Notes,x.Status,x.CreatedDate)).ToListAsync(token);
        var unread = await db.ContactForms.CountAsync(x => !x.IsRead, token);
        var smtp = await db.SiteTables.AsNoTracking().OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync(token);
        bool Present(string key, string? fallback) => !string.IsNullOrWhiteSpace(configuration[key] ?? fallback);
        var configured = Present("Smtp:Host",smtp?.Host) && Present("Smtp:Username",smtp?.SmtpUsername ?? smtp?.Mail) && Present("Smtp:Password",smtp?.Password) && Present("Smtp:SenderEmail",smtp?.Mail) && int.TryParse(configuration["Smtp:Port"] ?? smtp?.Port,out var port) && port is 465 or 587;
        return new(todayCount,yesterdayCount,pending,customers,guests,services,revenue,unread,configured,trend,distribution,recent);
    }
}
