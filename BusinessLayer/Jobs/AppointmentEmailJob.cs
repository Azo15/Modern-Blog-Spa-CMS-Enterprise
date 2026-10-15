using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Jobs;

public sealed class AppointmentEmailJob(AppDbContext db, IAppointmentMailService mail, ILogger<AppointmentEmailJob> logger)
{
    [AutomaticRetry(Attempts = 3)]
    public async Task SendAsync(int deliveryId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var leaseToken = Guid.NewGuid().ToString("N");
        var acquired = await db.AppointmentEmailDeliveries.Where(x => x.Id == deliveryId && x.SentAtUtc == null &&
            (x.LeaseExpiresAtUtc == null || x.LeaseExpiresAtUtc < now))
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.LeaseToken, leaseToken)
                .SetProperty(x => x.LeaseExpiresAtUtc, now.AddMinutes(5))
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1), cancellationToken);
        if (acquired == 0) return;
        try
        {
            var delivery = await db.AppointmentEmailDeliveries.AsNoTracking().IgnoreQueryFilters()
                .Include(x => x.Appointment).SingleAsync(x => x.Id == deliveryId, cancellationToken);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(90));
            await mail.SendReceiptAsync(delivery.Appointment, delivery.MessageId, deadline.Token);
            await db.AppointmentEmailDeliveries.Where(x => x.Id == deliveryId && x.LeaseToken == leaseToken)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.SentAtUtc, DateTime.UtcNow)
                    .SetProperty(x => x.LeaseToken, (string?)null).SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null)
                    .SetProperty(x => x.LastError, (string?)null), cancellationToken);
        }
        catch (Exception error)
        {
            logger.LogError(error, "Appointment email delivery {DeliveryId} failed and will retry.", deliveryId);
            try
            {
                await db.AppointmentEmailDeliveries.Where(x => x.Id == deliveryId && x.LeaseToken == leaseToken)
                    .ExecuteUpdateAsync(update => update.SetProperty(x => x.LeaseToken, (string?)null)
                        .SetProperty(x => x.LeaseExpiresAtUtc, (DateTime?)null).SetProperty(x => x.LastError, error.GetType().Name), CancellationToken.None);
            }
            catch (Exception bookkeepingError)
            {
                logger.LogWarning(bookkeepingError, "Appointment delivery {DeliveryId} lease will expire for recovery.", deliveryId);
            }
            throw;
        }
    }
}
