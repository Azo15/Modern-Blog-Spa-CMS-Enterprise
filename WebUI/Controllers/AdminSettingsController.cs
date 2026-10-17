using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Models;
namespace WebUI.Controllers;
[Authorize(Policy = "RequireAdmin")]
public class AdminSettingsController(ISiteTableService settings, IDataProtectionProvider protection, IConfiguration configuration, UserManager<AppUser> users, IMemoryCache cache, ILogger<AdminSettingsController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Smtp(CancellationToken token)
    {
        var row = await settings.GetQueryable().AsNoTracking().OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync(token);
        return View(new SmtpSettingsForm { Host = row?.Host ?? "", Port = int.TryParse(row?.Port, out var port) ? port : 587, Username = row?.SmtpUsername ?? row?.Mail ?? "", Mail = row?.Mail ?? "", EnableSsl = true, HasPassword = !string.IsNullOrWhiteSpace(row?.Password) });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Smtp(SmtpSettingsForm model, CancellationToken token)
    {
        var row = await settings.GetQueryable().AsNoTracking().OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync(token);
        model.HasPassword = !string.IsNullOrWhiteSpace(row?.Password);
        if (!model.HasPassword && string.IsNullOrWhiteSpace(model.Password)) ModelState.AddModelError(nameof(model.Password), "İlk kurulum için parola gereklidir.");
        if (!ModelState.IsValid)
        {
            model.Password = null;
            ModelState.Remove(nameof(model.Password));
            ModelState.AddModelError("", "Ayarlar kaydedilmedi. Alanları kontrol edin ve gerekiyorsa parolayı yeniden girin.");
            return View(model);
        }
        var creating = row is null;
        row ??= new SiteTable();
        row.Host = model.Host.Trim(); row.Port = model.Port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        row.Mail = model.Mail.Trim(); row.SmtpUsername = model.Username.Trim(); row.EnableSsl = true;
        if (!string.IsNullOrWhiteSpace(model.Password)) row.Password = SmtpSecret.Protect(protection, model.Password);
        else if (row.Password is { } previous) row.Password = SmtpSecret.Protect(protection, SmtpSecret.Read(protection, previous)!);
        var saved = creating ? await settings.AddAsync(row) : await settings.UpdateAsync(row);
        TempData["SmtpStatus"] = saved ? "SMTP ayarları güvenli olarak kaydedildi." : "Ayarlar kaydedilemedi.";
        return RedirectToAction(nameof(Smtp));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TestEmail(CancellationToken token)
    {
        var user = await users.GetUserAsync(User);
        if (user is null || !user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email)) return BadRequest(new { message = "Doğrulanmış yönetici e-postası gereklidir." });
        var key = "smtp-test:" + user.Id;
        lock (TestGate)
        {
            if (cache.TryGetValue(key, out object? _)) return StatusCode(429, new { message = "Yeni test için bir dakika bekleyin." });
            cache.Set(key, true, TimeSpan.FromMinutes(1));
        }
        try
        {
            var row = await settings.GetQueryable().AsNoTracking().OrderByDescending(x => x.CreatedDate).FirstOrDefaultAsync(token);
            var host = configuration["Smtp:Host"] ?? row?.Host;
            var username = configuration["Smtp:Username"] ?? row?.SmtpUsername ?? row?.Mail;
            var sender = configuration["Smtp:SenderEmail"] ?? row?.Mail;
            var password = configuration["Smtp:Password"] ?? SmtpSecret.Read(protection, row?.Password);
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(sender) || string.IsNullOrWhiteSpace(password) || !int.TryParse(configuration["Smtp:Port"] ?? row?.Port, out var port) || port is not (465 or 587)) return BadRequest(new { message = "Önce geçerli SMTP ayarlarını kaydedin." });
            var mail = new MimeMessage { Subject = "XSPA · SMTP bağlantı testi" };
            mail.From.Add(new MailboxAddress("XSPA", sender)); mail.To.Add(new MailboxAddress(user.UserName, user.Email));
            mail.Body = new TextPart("plain") { Text = "XSPA SMTP bağlantısı başarılı. Bu ileti yönetici hesabınızdan istenen bağlantı testidir." };
            using var client = new SmtpClient { Timeout = 15000 };
            await client.ConnectAsync(host, port, port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls, token);
            await client.AuthenticateAsync(username, password, token); await client.SendAsync(mail, token); await client.DisconnectAsync(true, token);
            return Json(new { message = "Test e-postası doğrulanmış yönetici adresinize gönderildi." });
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogWarning("SMTP bağlantı testi başarısız: {ErrorType}", error.GetType().Name);
            return StatusCode(502, new { message = "SMTP bağlantısı doğrulanamadı. Sunucu, port ve kimlik bilgilerini kontrol edin." });
        }
    }
    private static readonly object TestGate = new();
}
