using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusinessLayer.Concrete;

public sealed class ContactSubmissionService(AppDbContext db, IContactEmailQueue queue,
    ILogger<ContactSubmissionService> logger) : IContactSubmissionService
{
    public async Task<ContactSubmissionResult> SubmitAsync(ContactForm form, CancellationToken cancellationToken = default)
    {
        // Never accept audit, moderation or identity fields from the anonymous form.
        var message = new ContactForm
        {
            FullName = form.FullName?.Trim(), Email = form.Email?.Trim(), Phone = form.Phone?.Trim(),
            Subject = form.Subject?.Trim(), Message = form.Message?.Trim()
        };
        var delivery = new ContactEmailDelivery { ContactForm = message };
        db.ContactForms.Add(message);
        db.Set<ContactEmailDelivery>().Add(delivery);
        // EF writes the message and intent atomically. SMTP and Hangfire are outside this transaction.
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            var jobId = queue.Enqueue(delivery.Id);
            var now = DateTime.UtcNow;
            await db.Set<ContactEmailDelivery>().Where(x => x.Id == delivery.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.HangfireJobId, jobId)
                    .SetProperty(x => x.LastEnqueuedAtUtc, now), cancellationToken);
            return new(message.Id, true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The recovery job will discover this durable intent even when Hangfire is temporarily unavailable.
            logger.LogError(ex, "Contact {ContactFormId} was saved; email enqueue will be recovered.", message.Id);
            return new(message.Id, false);
        }
    }
}
