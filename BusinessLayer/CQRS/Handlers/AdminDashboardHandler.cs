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
using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
namespace BusinessLayer.CQRS.Handlers;
public sealed class AdminDashboardHandler(AppDbContext db, ICurrentUserService current, IAuthorizationService authorization, IConfiguration configuration,
    IVisitorAnalytics visitorAnalytics, IActiveMemberService members)
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    public async Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken token)
    {
        if (!(await authorization.AuthorizeAsync(current.User, AccessPolicies.ContentManager)).Succeeded) throw new UnauthorizedAccessException();
        await members.RequireAsync(token);
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
        var distributionRows = await active.GroupBy(x => x.ServiceTitleSnapshot).Select(x => new { Service = x.Key, Count = x.Count() }).OrderByDescending(x => x.Count).Take(8).ToListAsync(token);
        var distribution = distributionRows.Select(x => new DashboardDistribution(x.Service, x.Count)).ToList();
        var recent = await db.Appointments.AsNoTracking().OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id).Take(8)
            .Select(x => new AppointmentAdminDto(x.Id,x.ReferenceCode,x.FullName,x.Phone,x.Email,x.ServiceTitleSnapshot,x.AppointmentDate,x.TimeSlot,x.GuestCount,x.Notes,x.Status,x.CreatedDate,x.GiftCode)).ToListAsync(token);
        var unread = await db.ContactForms.CountAsync(x => !x.IsRead, token);
        var smtp = await db.SiteTables.AsNoTracking().OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync(token);
        bool Present(string key, string? fallback) => !string.IsNullOrWhiteSpace(configuration[key] ?? fallback);
        var configured = Present("Smtp:Host",smtp?.Host) && Present("Smtp:Username",smtp?.SmtpUsername ?? smtp?.Mail) && Present("Smtp:Password",smtp?.Password) && Present("Smtp:SenderEmail",smtp?.Mail) && int.TryParse(configuration["Smtp:Port"] ?? smtp?.Port,out var port) && port is 465 or 587;
        return new(todayCount,yesterdayCount,pending,customers,guests,services,revenue,unread,configured,trend,distribution,recent)
        { Analytics = await BuildAnalytics(today, token) };
    }
    private async Task<VisitorDashboardDto> BuildAnalytics(DateOnly today, CancellationToken ct)
    {
        var current = await visitorAnalytics.GetDayAsync(today, ct);
        var previous = await visitorAnalytics.GetDayAsync(today.AddDays(-1), ct);
        var trend = new List<DashboardDay>(); var start = today.AddDays(-13);
        for (var i = 0; i < 14; i++)
        {
            var day = start.AddDays(i); var snapshot = await visitorAnalytics.GetDayAsync(day, ct);
            trend.Add(new(day.ToString("dd.MM", CultureInfo.InvariantCulture), snapshot.UniqueVisitors));
        }
        var archivedViews = await db.DailyVisitorStats.Where(x => x.Date <= today).SumAsync(x => (long?)x.TotalPageViews, ct) ?? 0;
        var unarchivedViews = await db.VisitorLogs.Where(x => x.Date <= today && !db.DailyVisitorStats.Any(s => s.Date == x.Date)).LongCountAsync(ct);
        var top = await db.VisitorLogs.Where(x => x.Date >= start && x.Date <= today).GroupBy(x => x.Path)
            .Select(g => new { Path = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).ThenBy(x => x.Path).Take(5).ToListAsync(ct);
        var messages = await db.ContactForms.CountAsync(ct); var replied = await db.ContactForms.CountAsync(x => x.IsReplied, ct);
        var from = VisitorAnalyticsCalendar.StartUtc(today); var until = VisitorAnalyticsCalendar.StartUtc(today.AddDays(1));
        var requests = await db.Appointments.CountAsync(x => x.CreatedDate >= from && x.CreatedDate < until, ct);
        static decimal Rate(long value, long total) => total == 0 ? 0 : Math.Round(value * 100m / total, 1);
        return new()
        {
            TodayUniqueVisitors = current.UniqueVisitors, YesterdayUniqueVisitors = previous.UniqueVisitors,
            UniqueVisitorChangePercent = previous.UniqueVisitors == 0 ? current.UniqueVisitors == 0 ? 0 : null : Math.Round((current.UniqueVisitors - previous.UniqueVisitors) * 100m / previous.UniqueVisitors, 1),
            TodayPageViews = current.TotalPageViews, TotalPageViews = archivedViews + unarchivedViews,
            MobileVisits = current.MobileVisits, DesktopVisits = current.DesktopVisits,
            MobilePercent = Rate(current.MobileVisits, current.TotalPageViews), DesktopPercent = Rate(current.DesktopVisits, current.TotalPageViews),
            TotalMessages = messages, RepliedMessages = replied, MessageReplyRate = Rate(replied, messages),
            TodayAppointmentRequests = requests, AppointmentConversionRate = Rate(requests, current.UniqueVisitors),
            Trend = trend, TopPages = top.Select(x => new VisitedPageCount(x.Path, x.Count)).ToList()
        };
    }
}
