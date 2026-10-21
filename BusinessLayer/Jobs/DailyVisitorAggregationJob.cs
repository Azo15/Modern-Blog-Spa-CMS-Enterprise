using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using DataAccesLayer.Concrete;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Jobs;
public sealed class DailyVisitorAggregationJob(AppDbContext db, IVisitorAnalytics analytics,
    ILogger<DailyVisitorAggregationJob> logger, TimeProvider? timeProvider = null)
{
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(300)]
    public async Task RunAsync(CancellationToken ct)
    {
        var now = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        var today = VisitorAnalyticsCalendar.Day(now);
        try
        {
            var days = await db.VisitorLogs.Where(x => x.Date < today).Select(x => x.Date).Distinct().ToListAsync(ct);
            foreach (var day in days.Append(today.AddDays(-1)).Distinct().OrderBy(x => x)) await analytics.FinalizeAsync(day, ct);
            var cutoff = now.AddDays(-30);
            // Only finalized days are eligible for removal; a failed aggregation never purges its raw data.
            await db.VisitorLogs.Where(x => x.CreatedDate < cutoff && db.DailyVisitorStats.Any(s => s.Date == x.Date)).ExecuteDeleteAsync(ct);
            await analytics.GetDayAsync(today, ct); await analytics.GetDayAsync(today.AddDays(-1), ct);
            logger.LogInformation("Daily visitor aggregation completed for {Day}.", today.AddDays(-1));
        }
        catch (Exception error) when (error is not OperationCanceledException)
        { logger.LogError("Daily visitor aggregation failed ({ErrorType}); raw data is retained for retry.", error.GetType().Name); throw; }
    }
}
