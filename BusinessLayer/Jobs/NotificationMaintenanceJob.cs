using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Enum;
using Hangfire;
using Microsoft.EntityFrameworkCore;
namespace BusinessLayer.Jobs;
public sealed class NotificationMaintenanceJob(AppDbContext db, INotificationService notifications)
{
    [DisableConcurrentExecution(60)]
    public async Task RunAsync(CancellationToken ct)
    {
        // Durable source records repair a missed post-commit notification after a crash.
        var messages = await db.ContactForms.AsNoTracking().Where(x => !db.Notifications.Any(n => n.Type == NotificationType.ContactMessage && n.RelatedRecordId == x.Id)).OrderBy(x => x.Id).Take(100).Select(x => x.Id).ToListAsync(ct);
        foreach (var id in messages) await notifications.PublishAsync(new($"contact:{id}","Yeni iletişim mesajı","Yeni bir mesaj incelemenizi bekliyor.",NotificationType.ContactMessage,id),ct);
        var appointments = await db.Appointments.AsNoTracking().Where(x => !db.Notifications.Any(n => n.Type == NotificationType.AppointmentRequest && n.RelatedRecordId == x.Id)).OrderBy(x => x.Id).Take(100).Select(x => x.Id).ToListAsync(ct);
        foreach (var id in appointments) await notifications.PublishAsync(new($"appointment:{id}","Yeni randevu talebi","Yeni bir randevu talebi incelemenizi bekliyor.",NotificationType.AppointmentRequest,id),ct);
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var waiting = await db.ContactForms.AsNoTracking().Where(x => (!x.IsRead || !x.IsReplied) && x.CreatedDate < cutoff && !db.Notifications.Any(n => n.Type == NotificationType.MessageReminder && n.RelatedRecordId == x.Id)).OrderBy(x => x.Id).Take(100).Select(x => x.Id).ToListAsync(ct);
        foreach (var id in waiting) await notifications.PublishAsync(new($"waiting:{id}","Mesaj ilginizi bekliyor","24 saati geçen okunmamış veya yanıtlanmamış bir mesaj var.",NotificationType.MessageReminder,id),ct);
        var contacts = await db.ContactEmailDeliveries.IgnoreQueryFilters().AsNoTracking().Where(x => x.SentAtUtc == null && x.LastError != null && !db.Notifications.Any(n => n.EventKey == "email:contact:" + x.Id)).OrderBy(x => x.Id).Take(100).Select(x => x.Id).ToListAsync(ct);
        foreach (var id in contacts) await EmailAsync("contact",id,ct);
        var bookings = await db.AppointmentEmailDeliveries.IgnoreQueryFilters().AsNoTracking().Where(x => x.SentAtUtc == null && x.LastError != null && !db.Notifications.Any(n => n.EventKey == "email:appointment:" + x.Id)).OrderBy(x => x.Id).Take(100).Select(x => x.Id).ToListAsync(ct);
        foreach (var id in bookings) await EmailAsync("appointment",id,ct);
        var replies = await db.ContactReplies.IgnoreQueryFilters().AsNoTracking().Where(x => !x.IsMailSent && x.LastError != null && !db.Notifications.Any(n => n.EventKey == "email:reply:" + x.Id)).OrderBy(x => x.Id).Take(100).Select(x => x.Id).ToListAsync(ct);
        foreach (var id in replies) await EmailAsync("reply",id,ct);
    }
    private Task EmailAsync(string source,int id,CancellationToken ct) => notifications.PublishAsync(new($"email:{source}:{id}","E-posta gönderimi başarısız","Bir e-posta gönderilemedi. Arka Plan İşleri sayfasından teslimatı kontrol edin.",NotificationType.EmailFailure,id),ct);
}
