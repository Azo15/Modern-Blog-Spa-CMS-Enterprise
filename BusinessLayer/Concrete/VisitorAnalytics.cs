using System.Text.Json;
using BusinessLayer.Abstract;
using BusinessLayer.Dtos;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BusinessLayer.Concrete;
public sealed class VisitorAnalytics(AppDbContext db, IMemoryCache cache) : IVisitorAnalytics
{
    private static string Key(DateOnly day) => $"visitor-day:{day:yyyy-MM-dd}";
    public async Task RecordAsync(DateOnly day, string visitorHash, string path, bool mobile, DateTime createdUtc, CancellationToken ct)
    {
        db.VisitorLogs.Add(new VisitorLog { Date = day, VisitorHash = visitorHash, Path = path, IsMobile = mobile, CreatedDate = createdUtc });
        await db.SaveChangesAsync(ct); cache.Remove(Key(day));
    }
    public async Task<VisitorDaySnapshot> GetDayAsync(DateOnly day, CancellationToken ct)
    {
        if (cache.TryGetValue<VisitorDaySnapshot>(Key(day), out var cached) && cached is not null) return cached;
        var archived = await db.DailyVisitorStats.AsNoTracking().SingleOrDefaultAsync(x => x.Date == day, ct);
        VisitorDaySnapshot result;
        if (archived is not null)
            result = new(day, archived.TotalPageViews, archived.UniqueVisitors, archived.MobileVisits, archived.DesktopVisits,
                JsonSerializer.Deserialize<List<VisitedPageCount>>(archived.TopVisitedPagesJson ?? "[]") ?? []);
        else result = await Calculate(day, ct);
        cache.Set(Key(day), result, TimeSpan.FromSeconds(30)); return result;
    }
    private async Task<VisitorDaySnapshot> Calculate(DateOnly day, CancellationToken ct)
    {
        var logs = db.VisitorLogs.AsNoTracking().Where(x => x.Date == day);
        var groups = await logs.GroupBy(x => x.IsMobile).Select(g => new { Mobile = g.Key, Count = g.Count() }).ToListAsync(ct);
        var mobile = groups.Where(x => x.Mobile).Sum(x => x.Count); var desktop = groups.Where(x => !x.Mobile).Sum(x => x.Count);
        var unique = await logs.Select(x => x.VisitorHash).Distinct().CountAsync(ct);
        var pages = await logs.GroupBy(x => x.Path).Select(g => new { Path = g.Key, Count = g.Count() }).OrderByDescending(x => x.Count).ThenBy(x => x.Path).Take(5).ToListAsync(ct);
        return new(day, mobile + desktop, unique, mobile, desktop, pages.Select(x => new VisitedPageCount(x.Path, x.Count)).ToList());
    }
    public async Task FinalizeAsync(DateOnly day, CancellationToken ct)
    {
        // An archived day with no remaining raw logs must never be overwritten with zeros after retention cleanup.
        var existing = await db.DailyVisitorStats.SingleOrDefaultAsync(x => x.Date == day, ct);
        if (existing is not null && !await db.VisitorLogs.AnyAsync(x => x.Date == day, ct)) return;
        var result = await Calculate(day, ct); var row = existing ?? new DailyVisitorStat { Date = day };
        row.TotalPageViews = result.TotalPageViews; row.UniqueVisitors = result.UniqueVisitors;
        row.MobileVisits = result.MobileVisits; row.DesktopVisits = result.DesktopVisits; row.TopVisitedPagesJson = JsonSerializer.Serialize(result.TopPages);
        if (existing is null) db.DailyVisitorStats.Add(row);
        await db.SaveChangesAsync(ct); cache.Remove(Key(day));
    }
}
