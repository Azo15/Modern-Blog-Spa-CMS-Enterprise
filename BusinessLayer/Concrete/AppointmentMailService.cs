using System.Globalization;
using System.Net;
using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace BusinessLayer.Concrete;

public sealed class AppointmentMailService(ISiteTableService siteSettings, IConfiguration configuration) : IAppointmentMailService
{
    public async Task SendReceiptAsync(Appointment appointment, string messageId, CancellationToken cancellationToken = default)
    {
        var settings = await siteSettings.GetQueryable().AsNoTracking().OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync(cancellationToken);
        var host = configuration["Smtp:Host"] ?? settings?.Host;
        var portText = configuration["Smtp:Port"] ?? settings?.Port;
        var username = configuration["Smtp:Username"] ?? settings?.Mail;
        var password = configuration["Smtp:Password"] ?? settings?.Password;
        var sender = configuration["Smtp:SenderEmail"] ?? settings?.Mail;
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) || !int.TryParse(portText, out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("SMTP ayarları eksik veya geçersiz.");

        var date = appointment.AppointmentDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        var status = appointment.Status switch { AppointmentStatus.Confirmed => "Onaylandı", AppointmentStatus.Cancelled => "İptal edildi", _ => "İşletme onayı bekleniyor" };
        var message = new MimeMessage { Subject = $"XSPA randevu talebiniz — {appointment.ReferenceCode}", MessageId = messageId };
        message.From.Add(new MailboxAddress("XSPA Spa & Wellness", sender));
        message.To.Add(new MailboxAddress(appointment.FullName, appointment.Email));
        static string Encode(string value) => WebUtility.HtmlEncode(value);
        message.Body = new BodyBuilder
        {
            TextBody = $"Sayın {appointment.FullName},\n\nRandevu talebiniz alınmıştır.\nRezervasyon kodunuz: {appointment.ReferenceCode}\nHizmet: {appointment.ServiceTitleSnapshot}\nTarih / Saat: {date} {appointment.TimeSlot} (Türkiye saati)\nMisafir: {appointment.GuestCount}\nDurum: {status}\n\nTalebin alınması kesin randevu onayı değildir; ekibimiz uygunluğu sizinle teyit edecektir.\nDanışma: +90 538 878 70 00",
            HtmlBody = $"""
                <!doctype html><html lang="tr"><head><meta charset="utf-8"></head><body style="font-family:Arial,sans-serif;background:#f8f5ec;color:#173b2e;padding:32px">
                <h1 style="font-weight:normal">XSPA · Randevu Talebiniz</h1><p>Sayın {Encode(appointment.FullName)},</p>
                <p>Randevu talebiniz alınmıştır.</p><h2 style="color:#a78338">{Encode(appointment.ReferenceCode)}</h2>
                <table cellpadding="10" style="border:1px solid #d5bf85;border-collapse:collapse"><tr><th align="left">Hizmet</th><td>{Encode(appointment.ServiceTitleSnapshot)}</td></tr>
                <tr><th align="left">Tarih / Saat</th><td>{date} {Encode(appointment.TimeSlot)} · Türkiye saati</td></tr><tr><th align="left">Misafir</th><td>{appointment.GuestCount}</td></tr>
                <tr><th align="left">Durum</th><td>{status}</td></tr></table>
                <p>Talebin alınması kesin randevu onayı değildir; ekibimiz uygunluğu sizinle teyit edecektir.</p><p>Danışma: +90 538 878 70 00</p></body></html>
                """
        }.ToMessageBody();
        using var client = new SmtpClient { Timeout = 30000 };
        await client.ConnectAsync(host, port, port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, cancellationToken);
        await client.AuthenticateAsync(username, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        // Stable Message-Id plus a DB lease suppress normal retries. SMTP acceptance and DB commit
        // cannot be atomic: a crash between them can still require provider-side deduplication.
    }
}
