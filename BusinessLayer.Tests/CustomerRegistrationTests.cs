using System.Net;
using System.Text.RegularExpressions;
using BusinessLayer.Security;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace BusinessLayer.Tests;
public sealed class CustomerRegistrationTests
{
    private static HttpClient Client(PlatformWebFactory f)=>f.CreateClient(new WebApplicationFactoryClientOptions{BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false});
    private static string Token(string html)=>WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    private static Dictionary<string,string> Values(string token)=>new(){["FirstName"]="Customer",["LastName"]="Test",["Phone"]="+90 538 000 00 00",["Email"]="new-customer@example.test",["Password"]="CustomerPassword1!",["ConfirmPassword"]="CustomerPassword1!",["KvkkAccepted"]="true",["Role"]="System Admin",["__RequestVerificationToken"]=token};
    [Fact]
    public async Task RegistrationCheckboxUsesBooleanClientValidationInsteadOfNumericRange()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var c = Client(f);
        var html = await c.GetStringAsync("/dashboard/kayit");
        var checkbox = Regex.Match(html, "<input[^>]*id=\"KvkkAccepted\"[^>]*>").Value;
        Assert.NotEmpty(checkbox);
        Assert.Contains("data-val-mustbetrue=", checkbox);
        Assert.DoesNotContain("data-val-range", checkbox);
        Assert.Contains("acknowledgement-validation.js", html);
    }

    [Fact]
    public async Task PublicRegistrationAlwaysAssignsCustomerAndRecordsNoticeAcknowledgement()
    {
        using var f=new PlatformWebFactory();await f.InitializeAsync();using var c=Client(f);
        var html=await c.GetStringAsync("/dashboard/kayit");Assert.Contains("Google",html);
        using var response=await c.PostAsync("/dashboard/kayit",new FormUrlEncodedContent(Values(Token(html))));Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        using var scope=f.Services.CreateScope();var users=scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();var user=await users.FindByEmailAsync("new-customer@example.test");Assert.NotNull(user);
        Assert.Equal(AppRoles.Customer,Assert.Single(await users.GetRolesAsync(user)));Assert.Equal("+90 538 000 00 00",user.PhoneNumber);Assert.NotNull(user.PrivacyAcknowledgedAtUtc);Assert.Equal("2026-10-18",user.PrivacyNoticeVersion);
    }
    [Theory]
    [InlineData("KvkkAccepted","false")] [InlineData("Phone","")] [InlineData("ConfirmPassword","Different1!")]
    public async Task MissingConsentPhoneOrPasswordMatchCannotCreateAccount(string field,string value)
    {
        using var f=new PlatformWebFactory();await f.InitializeAsync();using var c=Client(f);var values=Values(Token(await c.GetStringAsync("/dashboard/kayit")));values[field]=value;
        using var response=await c.PostAsync("/dashboard/kayit",new FormUrlEncodedContent(values));Assert.Equal(HttpStatusCode.OK,response.StatusCode);
        using var scope=f.Services.CreateScope();Assert.False(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.AnyAsync(x=>x.Email=="new-customer@example.test"));
    }
    [Fact]
    public async Task UnconfiguredGoogleFailsGracefullyAndMissingCallbackIdentityCreatesNothing()
    {
        using var f=new PlatformWebFactory();await f.InitializeAsync();using var c=Client(f);var token=Token(await c.GetStringAsync("/dashboard/giris"));
        using var start=await c.PostAsync("/account/google/start",new FormUrlEncodedContent(new Dictionary<string,string>{["__RequestVerificationToken"]=token}));Assert.Equal(HttpStatusCode.Redirect,start.StatusCode);Assert.Equal("/dashboard/giris",start.Headers.Location?.OriginalString);
        using var callback=await c.GetAsync("/account/google/callback");Assert.Equal(HttpStatusCode.Redirect,callback.StatusCode);
        using var noCsrf=await c.PostAsync("/account/google/start",new FormUrlEncodedContent(new Dictionary<string,string>()));Assert.Equal(HttpStatusCode.BadRequest,noCsrf.StatusCode);
    }
    [Fact]
    public async Task StaffCanAccessSettingsButCannotReadOrPostRoleAssignments()
    {
        using var f=new PlatformWebFactory();await f.InitializeAsync();using var c=Client(f);var token=Token(await c.GetStringAsync("/dashboard/giris"));
        using var login=await c.PostAsync("/dashboard/giris",new FormUrlEncodedContent(new Dictionary<string,string>{["Email"]="editor@acceptance.test",["Password"]=PlatformWebFactory.Password,["__RequestVerificationToken"]=token}));Assert.Equal(HttpStatusCode.Redirect,login.StatusCode);
        Assert.Equal(HttpStatusCode.OK,(await c.GetAsync("/AdminSettings/Smtp")).StatusCode);
        var page=await c.GetStringAsync("/AdminDashboard");Assert.DoesNotContain("/AdminUserRole",page);
        using var get=await c.GetAsync("/AdminUserRole/AssignRole/5");Assert.Equal(HttpStatusCode.Forbidden,get.StatusCode);
        using var post=await c.PostAsync("/AdminUserRole/AssignRole",new FormUrlEncodedContent(new Dictionary<string,string>{["userId"]="5",["__RequestVerificationToken"]=Token(page)}));Assert.Equal(HttpStatusCode.Forbidden,post.StatusCode);
    }
}
