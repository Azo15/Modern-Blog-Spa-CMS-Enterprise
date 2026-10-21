using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace BusinessLayer.Jobs;
public sealed class ContactReplyRecoveryJob(AppDbContext db, IContactReplyQueue queue, ILogger<ContactReplyRecoveryJob> logger)
{
    [DisableConcurrentExecution(60)]
    public async Task RunAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow; var cutoff = now.AddHours(-1);
        var ids = await db.ContactReplies.IgnoreQueryFilters().Where(x => !x.IsMailSent && x.AttemptCount < 4 &&
            (x.LeaseExpiresAtUtc == null || x.LeaseExpiresAtUtc < now) && (x.LastEnqueuedAtUtc == null || x.LastEnqueuedAtUtc < cutoff))
            .OrderBy(x => x.Id).Select(x => x.Id).Take(100).ToListAsync(ct);
        foreach (var id in ids)
        {
            try { var job = queue.Enqueue(id); await db.ContactReplies.IgnoreQueryFilters().Where(x => x.Id == id).ExecuteUpdateAsync(s => s.SetProperty(x => x.HangfireJobId, job).SetProperty(x => x.LastEnqueuedAtUtc, now), ct); }
            catch (Exception error) when (error is not OperationCanceledException) { logger.LogWarning("Reply {ReplyId} remains pending ({ErrorType}).", id, error.GetType().Name); }
        }
    }
}
