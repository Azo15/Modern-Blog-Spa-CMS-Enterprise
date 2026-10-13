using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Jobs;

public sealed class ContactEmailJob(AppDbContext db, IMailService mail, ILogger<ContactEmailJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task SendAsync(int deliveryId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var token = Guid.NewGuid().ToString("N");
        // An atomic database lease works across processes and can be reclaimed after a worker crash.
        var acquired = await db.Set<ContactEmailDelivery>()
            .Where(x => x.Id == deliveryId && x.SentAtUtc == null &&
                (x.LeaseExpiresAtUtc == null || x.LeaseExpiresAtUtc < now))
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseToken, token)
                .SetProperty(x => x.LeaseExpiresAtUtc, now.AddMinutes(5))
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), cancellationToken);
        if (acquired == 0) return;

        try
        {
            var delivery = await db.Set<ContactEmailDelivery>().AsNoTracking()
                .Include(x => x.ContactForm).IgnoreQueryFilters().SingleAsync(x => x.Id == deliveryId, cancellationToken);
            // The absolute SMTP operation deadline is shorter than the processing lease.
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(90));
            await mail.SendContactMailAsync(delivery.ContactForm, delivery.MessageId, deadline.Token);
            var sentAt = DateTime.UtcNow;
            await db.Set<ContactEmailDelivery>().Where(x => x.Id == deliveryId && x.LeaseToken == token)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.SentAtUtc, sentAt)
                    .SetProperty(x => x.LeaseToken, (string?)null)
                    .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                    .SetProperty(x => x.LastError, (string?)null), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Contact delivery {DeliveryId} failed and will be retried by Hangfire.", deliveryId);
            // Use an uncancelled token for bookkeeping so a cancelled worker leaves a reclaimable intent.
            try
            {
                // Error type only: exceptions can contain SMTP credentials or visitor data.
                var error = ex.GetType().Name;
                await db.Set<ContactEmailDelivery>().Where(x => x.Id == deliveryId && x.LeaseToken == token)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseToken, (string?)null)
                        .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                        .SetProperty(x => x.LastError, error), CancellationToken.None);
            }
            catch (Exception bookkeepingError)
            {
                logger.LogWarning(bookkeepingError, "Delivery {DeliveryId} lease will expire for recovery.", deliveryId);
            }
            throw;
        }
    }
}
