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
public class AdminSettingsController(ISiteTableService settings, IDataProtectionProvider protection) : Controller
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
}
