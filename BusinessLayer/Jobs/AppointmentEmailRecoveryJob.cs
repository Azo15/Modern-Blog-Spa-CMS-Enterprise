using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Jobs;

public sealed class AppointmentEmailRecoveryJob(AppDbContext db, IAppointmentEmailQueue queue, ILogger<AppointmentEmailRecoveryJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RecoverAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-1);
        var ids = await db.AppointmentEmailDeliveries.AsNoTracking()
            .Where(x => x.SentAtUtc == null && x.AttemptCount < 4 && (x.LeaseExpiresAtUtc == null || x.LeaseExpiresAtUtc < now) &&
                (x.LastEnqueuedAtUtc == null || x.LastEnqueuedAtUtc < cutoff))
            .OrderBy(x => x.Id).Select(x => x.Id).Take(100).ToListAsync(cancellationToken);
        foreach (var id in ids)
        {
            try
            {
                var jobId = queue.Enqueue(id);
                await db.AppointmentEmailDeliveries.Where(x => x.Id == id).ExecuteUpdateAsync(update => update
                    .SetProperty(x => x.HangfireJobId, jobId).SetProperty(x => x.LastEnqueuedAtUtc, now), cancellationToken);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                logger.LogWarning(error, "Appointment delivery {DeliveryId} remains available for recovery.", id);
            }
        }
    }
}
