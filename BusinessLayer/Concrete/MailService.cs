using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Net;

namespace BusinessLayer.Concrete;

public sealed class MailService(ISiteTableService siteTableService, IConfiguration configuration) : IMailService
{
    public async Task SendContactMailAsync(ContactForm model, string? messageId = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await siteTableService.GetQueryable().AsNoTracking()
            .OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync(cancellationToken);
        // Environment variables / User Secrets override the existing dashboard configuration.
        var host = configuration["Smtp:Host"] ?? settings?.Host;
        var portText = configuration["Smtp:Port"] ?? settings?.Port;
        var user = configuration["Smtp:Username"] ?? settings?.Mail;
        var password = configuration["Smtp:Password"] ?? settings?.Password;
        var sender = configuration["Smtp:SenderEmail"] ?? settings?.Mail;
        var recipient = configuration["Smtp:RecipientEmail"] ?? sender;
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(sender) ||
            string.IsNullOrWhiteSpace(recipient) || string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(password) || !int.TryParse(portText, out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("SMTP ayarları eksik veya geçersiz.");

        var message = new MimeMessage { Subject = $"İletişim Formu: {model.Subject}" };
        if (!string.IsNullOrWhiteSpace(messageId)) message.MessageId = messageId;
        message.From.Add(new MailboxAddress("Modern Blog İletişim", sender));
        message.To.Add(MailboxAddress.Parse(recipient));
        if (MailboxAddress.TryParse(model.Email, out var replyTo))
            message.ReplyTo.Add(new MailboxAddress(model.FullName, replyTo.Address));
        string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
        var content = Encode(model.Message).Replace("\r\n", "<br>").Replace("\n", "<br>");
        var html = $"""
            <!doctype html><html lang="tr"><head><meta charset="utf-8"></head>
            <body style="font-family:Arial,sans-serif;color:#24324a">
            <h2>İletişim Mesajı</h2>
            <table style="border-collapse:collapse;width:100%;max-width:720px">
            <tr><th style="text-align:left">Ad Soyad</th><td>{Encode(model.FullName)}</td></tr>
            <tr><th style="text-align:left">E-posta</th><td>{Encode(model.Email)}</td></tr>
            <tr><th style="text-align:left">Telefon</th><td>{Encode(model.Phone)}</td></tr>
            <tr><th style="text-align:left">Konu</th><td>{Encode(model.Subject)}</td></tr>
            <tr><th style="text-align:left">Mesaj</th><td>{content}</td></tr>
            </table></body></html>
            """;
        message.Body = new BodyBuilder
        {
            TextBody = $"Ad Soyad: {model.FullName}\nE-posta: {model.Email}\nKonu: {model.Subject}\n\n{model.Message}",
            HtmlBody = html
        }.ToMessageBody();
        using var client = new SmtpClient { Timeout = 30000 };
        var secureSocketOptions = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(host, port, secureSocketOptions, cancellationToken);
        await client.AuthenticateAsync(user, password, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        // Sent marking belongs to ContactEmailJob. SMTP delivery cannot provide an atomic DB transaction;
        // a crash after server acceptance can still duplicate mail. Stable Message-Id supports deduplication.
    }
}
