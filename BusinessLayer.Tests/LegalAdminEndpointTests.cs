using System.Net;
using System.Text.RegularExpressions;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace BusinessLayer.Tests;
public sealed class LegalAdminEndpointTests
{
    private static HttpClient Client(PlatformWebFactory f) => f.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress=new Uri("https://localhost"),AllowAutoRedirect=false });
    private static string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    private static async Task Login(HttpClient c,string email)
    {
        var html=await c.GetStringAsync("/dashboard/giris");
        using var response=await c.PostAsync("/dashboard/giris",new FormUrlEncodedContent(new Dictionary<string,string>{["Email"]=email,["Password"]=PlatformWebFactory.Password,["__RequestVerificationToken"]=Token(html)}));
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
    }
    [Theory]
    [InlineData("/gizlilik-politikasi","Gizlilik Politikası")]
    [InlineData("/kullanim-kosullari","Kullanım Koşulları")]
    [InlineData("/kvkk","KVKK Aydınlatma Metni")]
    public async Task LegalPagesAreAnonymousAndFooterUsesRealRoutes(string path,string title)
    {
        using var f=new PlatformWebFactory(); await f.InitializeAsync(); using var c=Client(f);
        var html=WebUtility.HtmlDecode(await c.GetStringAsync(path));
        Assert.Contains(title,html); Assert.Contains("href=\"/kvkk\"",html); Assert.Contains("name=\"description\"",html);
    }
    [Theory]
    [InlineData("author@acceptance.test")] [InlineData("editor@acceptance.test")]
    public async Task NonAdminsCannotManageSmtp(string email)
    {
        using var f=new PlatformWebFactory(); await f.InitializeAsync(); using var c=Client(f); await Login(c,email);
        using var response=await c.GetAsync("/AdminSettings/Smtp");
        Assert.True(response.StatusCode==HttpStatusCode.Forbidden || response.StatusCode==HttpStatusCode.Redirect);
        Assert.DoesNotContain("smtp-test",await response.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task SmtpSecretIsNeverRenderedAndTestEndpointRequiresCsrf()
    {
        using var f=new PlatformWebFactory(); await f.InitializeAsync();
        using(var scope=f.Services.CreateScope()) {var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();db.SiteTables.Add(new SiteTable {Host="smtp.example.test",Port="587",Mail="sender@example.test",Password="legacy-secret-marker"});await db.SaveChangesAsync();}
        using var c=Client(f);await Login(c,"admin@acceptance.test");
        var html=await c.GetStringAsync("/AdminSettings/Smtp");Assert.DoesNotContain("legacy-secret-marker",html);
        Assert.DoesNotContain("legacy-secret-marker",await c.GetStringAsync("/AdminSiteInfo"));
        using var response=await c.PostAsync("/AdminSettings/TestEmail",new FormUrlEncodedContent(new Dictionary<string,string>()));Assert.Equal(HttpStatusCode.BadRequest,response.StatusCode);
        var dashboard=await c.GetStringAsync("/AdminDashboard");Assert.Contains("appointment-trend",dashboard);
    }
}
