using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Jobs;
public sealed class ContactReplyEmailJob(AppDbContext db, IContactReplyMailService mail, ILogger<ContactReplyEmailJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task SendAsync(int id, CancellationToken ct)
    {
        var now = DateTime.UtcNow; var lease = Guid.NewGuid().ToString("N");
        var rows = db.ContactReplies.IgnoreQueryFilters();
        if (await rows.Where(x => x.Id == id && !x.IsMailSent && (x.LeaseExpiresAtUtc == null || x.LeaseExpiresAtUtc < now))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseToken, lease).SetProperty(x => x.LeaseExpiresAtUtc, now.AddMinutes(5)).SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), ct) == 0) return;
        try
        {
            var reply = await rows.AsNoTracking().SingleAsync(x => x.Id == id, ct);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(TimeSpan.FromSeconds(90));
            await mail.SendAsync(reply, deadline.Token);
            await rows.Where(x => x.Id == id && x.LeaseToken == lease).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsMailSent, true)
                .SetProperty(x => x.SentAtUtc, DateTime.UtcNow).SetProperty(x => x.LeaseToken, (string?)null).SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null).SetProperty(x => x.LastError, (string?)null), ct);
        }
        catch (Exception error)
        {
            logger.LogWarning("Reply {ReplyId} delivery failed ({ErrorType}); retry retained.", id, error.GetType().Name);
            await rows.Where(x => x.Id == id && x.LeaseToken == lease).ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseToken, (string?)null)
                .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null).SetProperty(x => x.LastError, error.GetType().Name), CancellationToken.None);
            throw;
        }
    }
}
