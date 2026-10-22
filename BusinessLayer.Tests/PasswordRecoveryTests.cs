using System.Net;
using System.Text;
using BusinessLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;
namespace BusinessLayer.Tests;
public sealed class PasswordRecoveryTests
{
    [Fact]
    public async Task ForgotPasswordRequiresCsrfUsesTrustedOriginAndReturnsSameResponseForUnknownAddress()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var c = AjaxInteractionEndpointTests.Client(f);
        var data = new Dictionary<string,string> { ["Email"] = "admin@acceptance.test" };
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/dashboard/sifremi-unuttum", new FormUrlEncodedContent(data))).StatusCode);
        data["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(c);
        var known = await c.PostAsync("/dashboard/sifremi-unuttum", new FormUrlEncodedContent(data));
        data["Email"] = "missing@example.test";
        var unknown = await c.PostAsync("/dashboard/sifremi-unuttum", new FormUrlEncodedContent(data));
        Assert.Equal(HttpStatusCode.Redirect, known.StatusCode); Assert.Equal(known.StatusCode, unknown.StatusCode); Assert.Equal(known.Headers.Location, unknown.Headers.Location);
        f.ResetMail.Verify(x => x.SendAsync("admin@acceptance.test", It.Is<string>(url => url.StartsWith("https://xspawellness.runasp.net/dashboard/sifre-sifirla?") && url.Contains("token=")), It.IsAny<CancellationToken>()), Times.Once);
        f.ResetMail.Verify(x => x.SendAsync("missing@example.test", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    [Fact]
    public async Task ResetTokenChangesPasswordOnceAndCannotBeUsedForAnotherUser()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var c = AjaxInteractionEndpointTests.Client(f);
        string token;
        using (var scope = f.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await users.GeneratePasswordResetTokenAsync((await users.FindByIdAsync("1"))!)));
        }
        var data = new Dictionary<string,string> { ["UserId"] = "1", ["Token"] = token, ["Password"] = "Changed-password-123!", ["ConfirmPassword"] = "Changed-password-123!" };
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/dashboard/sifre-sifirla", new FormUrlEncodedContent(data))).StatusCode);
        data["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(c);
        var response = await c.PostAsync("/dashboard/sifre-sifirla", new FormUrlEncodedContent(data)); Assert.Equal("/dashboard/giris", response.Headers.Location?.OriginalString);
        using var check = f.Services.CreateScope(); var manager = check.ServiceProvider.GetRequiredService<UserManager<AppUser>>(); var user = (await manager.FindByIdAsync("1"))!;
        Assert.True(await manager.CheckPasswordAsync(user, "Changed-password-123!")); Assert.False(await manager.CheckPasswordAsync(user, PlatformWebFactory.Password));
        var raw = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        Assert.False((await manager.ResetPasswordAsync(user, raw, "Another-password-123!")).Succeeded);
        Assert.False((await manager.ResetPasswordAsync((await manager.FindByIdAsync("2"))!, raw, "Another-password-123!")).Succeeded);
    }
    [Fact]
    public async Task MailFailureDoesNotExposeAccountExistence()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var c = AjaxInteractionEndpointTests.Client(f);
        f.ResetMail.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("secret transport detail"));
        var data = new Dictionary<string,string> { ["Email"] = "admin@acceptance.test", ["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(c) };
        var response = await c.PostAsync("/dashboard/sifremi-unuttum", new FormUrlEncodedContent(data)); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.DoesNotContain("secret", await response.Content.ReadAsStringAsync());
    }
    [Fact]
    public void ResetEmailEncodesLinkAndHasOnlyIntendedRecipient()
    {
        using var mail = PasswordResetMailService.CreateMessage("owner@example.test", "https://example.test/reset?a=1&token='test'");
        Assert.Equal("owner@example.test", Assert.Single(mail.To.Mailboxes).Address); Assert.Empty(mail.Cc); Assert.Empty(mail.Bcc);
        Assert.Contains("&amp;token=&#39;test&#39;", mail.HtmlBody); Assert.Contains("1 saat", mail.TextBody);
    }
}
