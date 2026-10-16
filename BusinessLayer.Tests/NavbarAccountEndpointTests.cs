using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BusinessLayer.Tests;

public sealed class NavbarAccountEndpointTests
{
    private static HttpClient Client(PlatformWebFactory factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true
    });

    private static string Header(string html)
    {
        var header = Regex.Match(html, "<header[^>]*id=\"site-header\"[\\s\\S]*?</header>").Value;
        Assert.NotEmpty(header);
        return WebUtility.HtmlDecode(header);
    }

    private static string Csrf(string html)
    {
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        return WebUtility.HtmlDecode(token);
    }

    private static async Task SignIn(HttpClient client, string email)
    {
        var page = await client.GetStringAsync("/dashboard/giris");
        using var response = await client.PostAsync("/dashboard/giris", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email, ["Password"] = PlatformWebFactory.Password,
            ["__RequestVerificationToken"] = Csrf(page)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Theory]
    [InlineData("tr-TR", "Giriş Yap")]
    [InlineData("en-US", "Sign in")]
    public async Task AnonymousAccountHasVisibleLocalizedLabelAndSignInRouteOutsideCollapsedNavigation(string culture, string label)
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using var client = Client(factory);
        client.DefaultRequestHeaders.Add("Cookie", $".AspNetCore.Culture=c={culture}|uic={culture}");

        var header = Header(await client.GetStringAsync("/"));

        Assert.Contains("luxe-account-signin", header);
        Assert.Contains("href=\"/dashboard/giris\"", header);
        Assert.Contains($"<span>{label}</span>", header);
        Assert.True(header.IndexOf("luxe-nav-account", StringComparison.Ordinal) < header.IndexOf("id=\"navbarNav\"", StringComparison.Ordinal));
        Assert.DoesNotContain("luxe-account-logout", header);
        Assert.DoesNotContain("luxe-admin-link", header);
    }

    [Theory]
    [InlineData("admin@acceptance.test", "/AdminUserList/UserList")]
    [InlineData("editor@acceptance.test", "/AdminBlog")]
    [InlineData("author@acceptance.test", "/AdminBlog")]
    [InlineData("moderator@acceptance.test", "/AdminComment")]
    [InlineData("user@acceptance.test", null)]
    public async Task CookieSessionShowsAccountNameAndOnlyAuthorizedPanelAndCsrfProtectedLogout(string email, string? panel)
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using var client = Client(factory); await SignIn(client, email);

        var header = Header(await client.GetStringAsync("/"));

        Assert.Contains($"<span class=\"luxe-account-label\">{email}</span>", header);
        Assert.Contains("Çıkış Yap", header);
        Assert.Contains("method=\"post\"", header);
        Assert.Contains("action=\"/dashboard\"", header);
        Assert.NotEmpty(Csrf(header));
        Assert.DoesNotContain("luxe-account-signin", header);
        if (panel is null) Assert.DoesNotContain("luxe-admin-link", header);
        else Assert.Contains($"href=\"{panel}\"", header);
    }

    [Fact]
    public async Task NavbarLogoutRejectsMissingCsrfThenClearsCookieSessionWithValidToken()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using var client = Client(factory); await SignIn(client, "admin@acceptance.test");
        var header = Header(await client.GetStringAsync("/"));

        using var rejected = await client.PostAsync("/dashboard", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Contains("luxe-account-label", Header(await client.GetStringAsync("/")));

        using var response = await client.PostAsync("/dashboard", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Csrf(header)
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var signedOut = Header(await client.GetStringAsync("/"));
        Assert.Contains("Giriş Yap", signedOut);
        Assert.DoesNotContain("luxe-admin-link", signedOut);
        Assert.DoesNotContain("luxe-account-logout", signedOut);
    }
}
