using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Jobs;

public sealed class ContactEmailRecoveryJob(AppDbContext db, IContactEmailQueue queue,
    ILogger<ContactEmailRecoveryJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task RecoverAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var retryCutoff = now.AddHours(-1);
        var ids = await db.Set<ContactEmailDelivery>().AsNoTracking()
            .Where(x => x.SentAtUtc == null && (x.LeaseExpiresAtUtc == null || x.LeaseExpiresAtUtc < now) &&
                (x.LastEnqueuedAtUtc == null || x.LastEnqueuedAtUtc < retryCutoff))
            .OrderBy(x => x.Id).Select(x => x.Id).Take(100).ToListAsync(cancellationToken);
        foreach (var id in ids)
        {
            try
            {
                var jobId = queue.Enqueue(id);
                await db.Set<ContactEmailDelivery>().Where(x => x.Id == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.HangfireJobId, jobId)
                        .SetProperty(x => x.LastEnqueuedAtUtc, now), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Delivery {DeliveryId} remains available for the next recovery run.", id);
            }
        }
    }
}
