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
    public async Task RecoveryViewsHaveCsrfAccessibleFieldsAndGenericConfirmation()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var c = AjaxInteractionEndpointTests.Client(f);
        Assert.Contains("/dashboard/sifremi-unuttum", await c.GetStringAsync("/dashboard/giris"));
        var forgot = await c.GetStringAsync("/dashboard/sifremi-unuttum"); Assert.Contains("__RequestVerificationToken", forgot); Assert.Contains("autocomplete=\"email\"", forgot);
        var missing = await c.GetAsync("/dashboard/sifre-sifirla"); Assert.Equal(HttpStatusCode.Redirect, missing.StatusCode);
        var reset = await c.GetAsync("/dashboard/sifre-sifirla?userId=1&token=test");
        Assert.Equal("no-referrer", Assert.Single(reset.Headers.GetValues("Referrer-Policy"))); Assert.True(reset.Headers.CacheControl!.NoStore);
        var html = await reset.Content.ReadAsStringAsync(); Assert.Contains("autocomplete=\"new-password\"", html); Assert.Contains("name=\"Token\"", html);
        var fields = new Dictionary<string,string> { ["Email"] = "missing@example.test", ["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(c) };
        await c.PostAsync("/dashboard/sifremi-unuttum", new FormUrlEncodedContent(fields));
        var notice = WebUtility.HtmlDecode(await c.GetStringAsync("/dashboard/sifremi-unuttum")); Assert.Contains("Eğer bu e-posta adresi sistemimizde kayıtlıysa", notice);
    }
    [Theory]
    [InlineData("1", "not-a-token", "matching-password", "matching-password")]
    [InlineData("not-an-id", "token", "matching-password", "matching-password")]
    [InlineData("1", "token", "matching-password", "different-password")]
    public async Task InvalidResetIsReadableAndDoesNotChangePassword(string id, string token, string password, string confirm)
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var c = AjaxInteractionEndpointTests.Client(f);
        var fields = new Dictionary<string,string> { ["UserId"] = id, ["Token"] = token, ["Password"] = password, ["ConfirmPassword"] = confirm, ["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(c) };
        var response = await c.PostAsync("/dashboard/sifre-sifirla", new FormUrlEncodedContent(fields)); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("validation-summary-errors", await response.Content.ReadAsStringAsync());
        using var scope = f.Services.CreateScope(); var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(); Assert.True(await users.CheckPasswordAsync((await users.FindByIdAsync("1"))!, PlatformWebFactory.Password));
    }
    [Fact]
    public void ResetEmailEncodesLinkAndHasOnlyIntendedRecipient()
    {
        using var mail = PasswordResetMailService.CreateMessage("owner@example.test", "https://example.test/reset?a=1&token='test'");
        Assert.Equal("owner@example.test", Assert.Single(mail.To.Mailboxes).Address); Assert.Empty(mail.Cc); Assert.Empty(mail.Bcc);
        Assert.Contains("&amp;token=&#39;test&#39;", mail.HtmlBody); Assert.Contains("1 saat", mail.TextBody);
    }
}
