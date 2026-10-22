using System.Net;
using BusinessLayer.Concrete;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace BusinessLayer.Tests;
public sealed class SmtpRecoveryEndpointTests
{
    [Fact]
    public async Task LostKeyShowsRecoveryNoticeAndBlankSavePreservesCiphertext()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var c = AjaxInteractionEndpointTests.Client(f);
        var encrypted = SmtpSecret.Protect(new EphemeralDataProtectionProvider(), "test-only-secret");
        using (var scope = f.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.SiteTables.Add(new SiteTable { Host = "smtp.gmail.com", Port = "587", Mail = "sender@example.test", SmtpUsername = "sender@example.test", Password = encrypted }); await db.SaveChangesAsync();
        }
        await c.PostAsync("/dashboard/giris", new FormUrlEncodedContent(new Dictionary<string,string> { ["Email"] = "admin@acceptance.test", ["Password"] = PlatformWebFactory.Password, ["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(c) }));
        var html = WebUtility.HtmlDecode(await c.GetStringAsync("/AdminSettings/Smtp"));
        Assert.Contains("mevcut anahtarla açılamıyor", html); Assert.DoesNotContain("test-only-secret", html); Assert.DoesNotContain(encrypted, html);
        var response = await c.PostAsync("/AdminSettings/Smtp", new FormUrlEncodedContent(new Dictionary<string,string> { ["Host"] = "smtp.gmail.com", ["Port"] = "587", ["Username"] = "sender@example.test", ["Mail"] = "sender@example.test", ["EnableSsl"] = "true", ["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(c, "/AdminSettings/Smtp") }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Google uygulama şifresini yeniden girip kaydedin", WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync()));
        using var check = f.Services.CreateScope(); Assert.Equal(encrypted, (await check.ServiceProvider.GetRequiredService<AppDbContext>().SiteTables.SingleAsync()).Password);
    }
}
