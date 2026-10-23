using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using BusinessLayer.Security;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace BusinessLayer.Tests;
public sealed class GoogleAccountFlowTests
{
    private static HttpClient Client(PlatformWebFactory factory,string email,bool verified)
    {
        var client=factory.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
        var options=factory.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(IdentityConstants.ExternalScheme);
        var claims=new[]{new Claim(ClaimTypes.NameIdentifier,"google-test-subject"),new Claim(ClaimTypes.Email,email),new Claim(ClaimTypes.GivenName,"Google"),new Claim(ClaimTypes.Surname,"Guest"),new Claim("urn:xspa:email_verified",verified?"true":"false")};
        var properties=new AuthenticationProperties();properties.Items["LoginProvider"]="Google";
        var ticket=new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims,IdentityConstants.ExternalScheme)),properties,IdentityConstants.ExternalScheme);
        client.DefaultRequestHeaders.Add("Cookie",options.Cookie.Name+"="+options.TicketDataFormat.Protect(ticket));return client;
    }
    [Fact]
    public async Task VerifiedExistingEmailLinksOnceAndKeepsRoleAndLocalRedirect()
    {
        using var factory=new PlatformWebFactory();await factory.InitializeAsync();using var client=Client(factory,"admin@acceptance.test",true);
        using var response=await client.GetAsync("/account/google/callback");Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);Assert.Equal("/Profile",response.Headers.Location?.OriginalString);
        using var scope=factory.Services.CreateScope();var users=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();var linked = await users.FindByLoginAsync("Google","google-test-subject"); Assert.NotNull(linked);
        Assert.True(linked.EmailConfirmed); Assert.True(await users.IsInRoleAsync(linked, AppRoles.Admin));
        using var again = await client.GetAsync("/account/google/callback?returnUrl=https://evil.example");
        Assert.Equal("/Profile", again.Headers.Location?.OriginalString); Assert.Single(await users.GetLoginsAsync(linked));
    }
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    public async Task LinkingRejectsInactiveLockedOrUnverifiedAccounts(bool active, bool locked, bool verified)
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(); var user = (await users.FindByEmailAsync("admin@acceptance.test"))!;
            user.IsActive = active; user.LockoutEnabled = true; user.LockoutEnd = locked ? DateTimeOffset.UtcNow.AddHours(1) : null; await users.UpdateAsync(user);
        }
        using var client = Client(factory, "admin@acceptance.test", verified);
        using var response = await client.GetAsync("/account/google/callback"); Assert.Equal("/dashboard/giris", response.Headers.Location?.OriginalString);
        using var check = factory.Services.CreateScope(); Assert.Null(await check.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByLoginAsync("Google", "google-test-subject"));
    }
    [Fact]
    public async Task UnverifiedGoogleEmailCannotStartRegistration()
    {
        using var factory=new PlatformWebFactory();await factory.InitializeAsync();using var client=Client(factory,"unverified@example.test",false);
        using var response=await client.GetAsync("/account/google/callback");Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
    }
    [Fact]
    public async Task VerifiedGoogleRegistrationRequiresNoticeAndCreatesOnlyCustomer()
    {
        using var factory=new PlatformWebFactory();await factory.InitializeAsync();using var client=Client(factory,"google-new@example.test",true);
        var html=await client.GetStringAsync("/account/google/callback?returnUrl=https://evil.example");Assert.Contains("google-new@example.test",html);
        var token=WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        var values=new Dictionary<string,string>{["FirstName"]="Google",["LastName"]="Guest",["Phone"]="05380000000",["KvkkAccepted"]="false",["ReturnUrl"]="https://evil.example",["Role"]="System Admin",["__RequestVerificationToken"]=token};
        using var rejected=await client.PostAsync("/account/google/register",new FormUrlEncodedContent(values));Assert.Equal(HttpStatusCode.OK,rejected.StatusCode);
        using(var scope=factory.Services.CreateScope())Assert.Null(await scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>().FindByEmailAsync("google-new@example.test"));
        values["KvkkAccepted"]="true";using var accepted=await client.PostAsync("/account/google/register",new FormUrlEncodedContent(values));Assert.Equal(HttpStatusCode.Redirect,accepted.StatusCode);Assert.Equal("/Profile",accepted.Headers.Location?.OriginalString);
        using var verify=factory.Services.CreateScope();var users=verify.ServiceProvider.GetRequiredService<UserManager<AppUser>>();var user=(await users.FindByEmailAsync("google-new@example.test"))!;
        using var repeat = Client(factory, "google-new@example.test", true);
        using var returned = await repeat.GetAsync("/account/google/callback?returnUrl=/randevularim");
        Assert.Equal("/randevularim", returned.Headers.Location?.OriginalString);
        using var stale = Client(factory, "google-new@example.test", true);
        var staleToken = await AjaxInteractionEndpointTests.Token(stale);
        using var stalePost = await stale.PostAsync("/account/google/register", new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = staleToken, ["ReturnUrl"] = "/Profile" }));
        Assert.StartsWith("/account/google/callback", stalePost.Headers.Location?.OriginalString);
        Assert.Equal(AppRoles.Customer,Assert.Single(await users.GetRolesAsync(user)));Assert.True(user.EmailConfirmed);Assert.NotNull(user.PrivacyAcknowledgedAtUtc);Assert.Single(await users.GetLoginsAsync(user));
    }
}
