using System.Net;
using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace BusinessLayer.Concrete;

public sealed class ContactReplyMailService(ISiteTableService settings, IConfiguration config, IDataProtectionProvider? protection = null) : IContactReplyMailService
{
    public static MimeMessage CreateMessage(ContactReply reply, string sender)
    {
        var mail = new MimeMessage { Subject = $"XSPA · Yanıt: {reply.Subject}", MessageId = reply.MessageId };
        mail.From.Add(new MailboxAddress("XSPA Spa & Wellness", sender));
        mail.To.Add(MailboxAddress.Parse(reply.RecipientEmail));
        var content = WebUtility.HtmlEncode(reply.ReplyMessage).Replace("\r\n", "<br>").Replace("\n", "<br>");
        mail.Body = new BodyBuilder { TextBody = $"XSPA ekibinden mesajınıza yanıt:\n\n{reply.ReplyMessage}\n\nXSPA Spa & Wellness · +90 538 878 70 00",
            HtmlBody = $"""
            <!doctype html><html lang="tr"><head><meta charset="utf-8"></head><body style="background:#f8f5ec;color:#173b2e;font:16px Arial;padding:30px"><div style="max-width:640px;margin:auto;border:1px solid #d4b86c"><header style="background:#102c22;color:#e4c77e;padding:32px;font:32px Georgia;letter-spacing:5px">XSPA <small style="display:block;font:12px Arial;letter-spacing:3px">SPA &amp; WELLNESS</small></header><main style="padding:30px"><h1 style="font:26px Georgia">Sizin için bir not</h1><p>Mesajınız için teşekkür ederiz.</p><div style="line-height:1.8;overflow-wrap:anywhere">{content}</div><hr style="border:0;border-top:1px solid #d4b86c;margin:28px 0"><p>XSPA Ekibi<br>+90 538 878 70 00</p></main></div></body></html>
            """ }.ToMessageBody();
        return mail;
    }
    public async Task SendAsync(ContactReply reply, CancellationToken ct)
    {
        var row = await settings.GetQueryable().AsNoTracking().OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync(ct);
        var host = config["Smtp:Host"] ?? row?.Host; var user = config["Smtp:Username"] ?? row?.SmtpUsername ?? row?.Mail;
        var password = config["Smtp:Password"] ?? SmtpSecret.Read(protection, row?.Password);
        var sender = config["Smtp:SenderEmail"] ?? row?.Mail;
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(sender) || !int.TryParse(config["Smtp:Port"] ?? row?.Port, out var port) || port is not (465 or 587))
            throw new InvalidOperationException("SMTP ayarları eksik veya geçersiz.");
        using var mail = CreateMessage(reply, sender); using var client = new SmtpClient { Timeout = 30000 };
        await client.ConnectAsync(host, port, port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(user, password, ct); await client.SendAsync(mail, ct); await client.DisconnectAsync(true, ct);
    }
}
