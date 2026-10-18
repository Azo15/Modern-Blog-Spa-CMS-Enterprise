using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BusinessLayer.Security;
using DataAccesLayer.Concrete;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer.Tests;

public sealed class ApiEndpointTests
{
    private static HttpClient Client(PlatformWebFactory factory) => factory.CreateClient(new WebApplicationFactoryClientOptions
    { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = false });

    private static object BlogBody => new { title = "Created through API", subTitle = "A validated long subtitle", content = "<p>Safe content</p><script>alert(1)</script>", categoryId = 1 };

    private static async Task Login(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = PlatformWebFactory.Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("data").GetProperty("accessToken").GetString());
    }

    private static async Task Problem(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal((int)expected, body.GetProperty("status").GetInt32());
        Assert.True(body.TryGetProperty("title", out _));
        Assert.DoesNotContain("<html", body.GetRawText(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PublicApiUsesReadOnlyPaginatedDtosAndSearchHidesDraftTrashAndPendingComments()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory);
        var page = await client.GetFromJsonAsync<JsonElement>("/api/v1/blogs?pageIndex=1&pageSize=1&categoryId=1&search=Massage");
        Assert.True(page.GetProperty("success").GetBoolean()); Assert.Equal(200, page.GetProperty("statusCode").GetInt32());
        Assert.Equal(JsonValueKind.Null, page.GetProperty("errors").ValueKind);
        var data = page.GetProperty("data"); Assert.Equal(1, data.GetProperty("totalCount").GetInt32());
        Assert.Equal("approved-massage", data.GetProperty("items")[0].GetProperty("slug").GetString());
        foreach (var field in new[] { "passwordHash", "securityStamp", "email", "createdBy", "deletedBy" }) Assert.DoesNotContain('"' + field + '"', page.GetRawText());
        var detail = await client.GetFromJsonAsync<JsonElement>("/api/v1/blogs/approved-massage");
        Assert.Contains("Approved root", detail.GetRawText()); Assert.Contains("Approved reply", detail.GetRawText());
        Assert.DoesNotContain("Private pending", detail.GetRawText());
        await Problem(await client.GetAsync("/api/v1/blogs/hidden-draft"), HttpStatusCode.NotFound);
        await Problem(await client.GetAsync("/api/v1/blogs/deleted-article"), HttpStatusCode.NotFound);
        var categories = await client.GetFromJsonAsync<JsonElement>("/api/v1/categories");
        Assert.True(categories.GetProperty("success").GetBoolean());
        Assert.Contains("Wellness", categories.GetRawText());
    }

    [Theory]
    [InlineData(AppRoles.Admin, "admin@acceptance.test", true, true, true)]
    [InlineData(AppRoles.Staff, "editor@acceptance.test", true, false, true)]
    [InlineData(AppRoles.Staff, "author@acceptance.test", true, false, false)]
    [InlineData(AppRoles.Staff, "moderator@acceptance.test", false, true, true)]
    [InlineData(AppRoles.Customer, "user@acceptance.test", false, false, false)]
    [InlineData(null, null, false, false, false)]
    public async Task SixProfilesExerciseRealAuthenticationBlogCommentModerationAndReportBoundaries(string? role, string? email, bool canCreate, bool canModerate, bool canReport)
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory);
        if (email is not null) await Login(client, email);
        var aboutCards = await client.GetAsync("/AdminAboutCard");
        Assert.Equal(role is AppRoles.Admin or AppRoles.Staff ? HttpStatusCode.OK : role is null ? HttpStatusCode.Redirect : HttpStatusCode.Forbidden, aboutCards.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/blogs")).StatusCode);
        using var create = await client.PostAsJsonAsync("/api/v1/blogs", BlogBody);
        if (canCreate)
        {
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            var wrapper = await create.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(201, wrapper.GetProperty("statusCode").GetInt32());
        }
        else await Problem(create, role is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden);
        using var comment = await client.PostAsJsonAsync("/api/v1/comments", new { blogId = 1, content = "Acceptance pending comment", parentCommentId = (int?)null });
        if (role is not null) Assert.Equal(HttpStatusCode.Created, comment.StatusCode); else await Problem(comment, HttpStatusCode.Unauthorized);
        var pending = await client.GetAsync("/AdminComment");
        Assert.Equal(canModerate ? HttpStatusCode.OK : role is null ? HttpStatusCode.Redirect : HttpStatusCode.Forbidden, pending.StatusCode);
        var reports = await client.GetAsync("/Reports/Contacts?format=Excel");
        Assert.Equal(canReport ? HttpStatusCode.OK : role is null ? HttpStatusCode.Redirect : HttpStatusCode.Forbidden, reports.StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.All(await db.Comments.Where(c => c.Content == "Acceptance pending comment").ToListAsync(), c => Assert.False(c.IsApproved));
        if (canCreate) Assert.Equal(EntityLayer.Enum.PublishStatus.Draft, (await db.Blogs.SingleAsync(b => b.Title == "Created through API")).PublishStatus);
    }

    [Fact]
    public async Task AuthorOwnershipAndOriginalAuthorAreEnforcedAcrossPutDeleteAndCreate()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory);
        await Login(client, "author@acceptance.test");
        await Problem(await client.PutAsJsonAsync("/api/v1/blogs/3", BlogBody), HttpStatusCode.Forbidden);
        await Problem(await client.DeleteAsync("/api/v1/blogs/3"), HttpStatusCode.Forbidden);
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/v1/blogs/1", BlogBody)).StatusCode);
        await Login(client, "editor@acceptance.test");
        Assert.Equal(HttpStatusCode.OK, (await client.PutAsJsonAsync("/api/v1/blogs/1", BlogBody)).StatusCode);
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var owned = await db.Blogs.AsNoTracking().SingleAsync(b => b.Id == 1); Assert.Equal(3, owned.AppUserId);
        Assert.DoesNotContain("<script", owned.Content);
        Assert.Equal(HttpStatusCode.OK, (await client.DeleteAsync("/api/v1/blogs/1")).StatusCode);
        Assert.True((await db.Blogs.IgnoreQueryFilters().AsNoTracking().SingleAsync(b => b.Id == 1)).IsDeleted);
    }

    [Theory]
    [InlineData("/api/v1/blogs?pageIndex=0", 400)]
    [InlineData("/api/v1/blogs/does-not-exist", 404)]
    [InlineData("/api/v1/missing", 404)]
    [InlineData("/api/v1/auth/me", 401)]
    public async Task ApiErrorsAlwaysReturnProblemJsonEvenWhenHtmlIsRequested(string path, int status)
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        await Problem(await client.GetAsync(path), (HttpStatusCode)status);
    }

    [Fact]
    public async Task MalformedJsonAndInvalidTokenCannotReachCommands()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory);
        await Problem(await client.PostAsync("/api/v1/auth/login", new StringContent("{ broken", Encoding.UTF8, "application/json")), HttpStatusCode.BadRequest);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-token");
        await Problem(await client.GetAsync("/api/v1/auth/me"), HttpStatusCode.Unauthorized);
        await Problem(await client.PostAsJsonAsync("/api/v1/blogs", BlogBody), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CookieCommandsRequireCsrfAndAnInvalidBearerNeverFallsBackToCookie()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory);
        var loginPage = await client.GetAsync("/dashboard/giris");
        var html = await loginPage.Content.ReadAsStringAsync();
        var token = System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        var cookie = string.Join("; ", loginPage.Headers.GetValues("Set-Cookie").Select(value => value.Split(';')[0]));
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        var login = await client.PostAsync("/dashboard/giris", new FormUrlEncodedContent(new Dictionary<string, string>
        { ["Email"] = "author@acceptance.test", ["Password"] = PlatformWebFactory.Password, ["__RequestVerificationToken"] = token }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookie + "; " + string.Join("; ", login.Headers.GetValues("Set-Cookie").Select(value => value.Split(';')[0])));
        await Problem(await client.PostAsJsonAsync("/api/v1/blogs", BlogBody), HttpStatusCode.BadRequest);
        var createPage = await client.GetStringAsync("/AdminBlog/Create");
        var csrf = System.Text.RegularExpressions.Regex.Match(createPage, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        client.DefaultRequestHeaders.Add("RequestVerificationToken", csrf);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/blogs", BlogBody)).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");
        await Problem(await client.PostAsJsonAsync("/api/v1/blogs", BlogBody), HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("lock")]
    [InlineData("stamp")]
    [InlineData("role")]
    [InlineData("inactive")]
    public async Task ExistingJwtStopsWorkingWhenAccountStateChanges(string change)
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory);
        await Login(client, "user@acceptance.test"); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<EntityLayer.Concrete.AppUser>>();
            var user = (await users.FindByIdAsync("5"))!;
            if (change == "lock") { await users.SetLockoutEnabledAsync(user, true); await users.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddDays(1)); }
            else if (change == "stamp") await users.UpdateSecurityStampAsync(user);
            else if (change == "role") await users.AddToRoleAsync(user, AppRoles.Staff);
            else { user.IsActive = false; await users.UpdateAsync(user); }
        }
        await Problem(await client.GetAsync("/api/v1/auth/me"), HttpStatusCode.Unauthorized);
        await Problem(await client.PostAsJsonAsync("/api/v1/comments", new { blogId = 1, content = "Must not save" }), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RateLimitProduces429ProblemWithRetryAfter()
    {
        using var factory = new PlatformWebFactory(permitLimit: 2); await factory.InitializeAsync(); using var client = Client(factory);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/blogs")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/blogs")).StatusCode);
        var denied = await client.GetAsync("/api/v1/blogs"); await Problem(denied, HttpStatusCode.TooManyRequests);
        Assert.True(denied.Headers.RetryAfter?.Delta > TimeSpan.Zero);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, (await client.GetAsync("/health/ready")).StatusCode);
    }

    [Fact]
    public async Task UnhandledApiFailureReturnsSanitized500Problem()
    {
        using var factory = new PlatformWebFactory(failQueries: true); await factory.InitializeAsync(); using var client = Client(factory);
        var response = await client.GetAsync("/api/v1/blogs"); await Problem(response, HttpStatusCode.InternalServerError);
        var text = await response.Content.ReadAsStringAsync(); Assert.DoesNotContain("super-secret", text); Assert.DoesNotContain("private@example", text);
    }

    [Fact]
    public async Task ContactPreviewHandlesShortAddressesAndEncodesUntrustedMarkup()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory);
        await Login(client, "admin@acceptance.test");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Contacts.Add(new EntityLayer.Concrete.Contact { Adres = "<img src=x onerror=unsafe-address()>" + new string('a', 60), Mail = "admin@example.test" });
            await db.SaveChangesAsync();
        }
        var page = await client.GetAsync("/AdminContact");
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        var html = await page.Content.ReadAsStringAsync();
        Assert.DoesNotContain("<img src=x onerror=unsafe-address()>", html);
        Assert.Contains("&lt;img", html);
    }

    [Fact]
    public async Task NewsletterInterestRequiresCsrfAndPersistsWithoutJavaScript()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
        var denied = await client.PostAsync("/iletisim/bulten", new FormUrlEncodedContent(new Dictionary<string,string> { ["email"]="reader@example.test" }));
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);
        var home = await client.GetStringAsync("/");
        var token = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(home, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(token);
        var response = await client.PostAsync("/iletisim/bulten", new FormUrlEncodedContent(new Dictionary<string,string>
            { ["email"]="reader@example.test", ["__RequestVerificationToken"]=token }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/iletisim", response.Headers.Location?.OriginalString);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("reader@example.test", Assert.Single(await db.ContactForms.ToListAsync()).Email);
        Assert.Single(await db.Set<EntityLayer.Concrete.ContactEmailDelivery>().ToListAsync());
    }

    [Fact]
    public async Task FreshDeploymentHomeWorksWithoutDemoDataAndLegacyRichTextIsSanitized()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Sliders.Add(new EntityLayer.Concrete.Slider { Title = "Security canary", Content = "<strong>Welcome</strong><script>unsafe-canary()</script><img src=x onerror='unsafe-image()'>" });
            db.Galaries.Add(new EntityLayer.Concrete.Galary { Title = "<img src=x onerror=gallery-attack()>", Image = "missing-demo.webp" });
            await db.SaveChangesAsync();
        }
        var home = await client.GetStringAsync("/");
        Assert.Contains("Welcome", home);
        Assert.DoesNotContain("unsafe-canary", home);
        Assert.DoesNotContain("unsafe-image", home);
        Assert.Contains("&amp;lt;img", home);
    }

    [Fact]
    public async Task SwaggerListsApiXmlDocsAndBearerDefinition()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory);
        var document = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        Assert.Equal("Modern Spa & Blog Platform API v1", document.GetProperty("info").GetProperty("title").GetString());
        Assert.True(document.GetProperty("components").GetProperty("securitySchemes").TryGetProperty("Bearer", out _));
        Assert.True(document.GetProperty("paths").TryGetProperty("/api/v1/auth/login", out _));
        Assert.False(document.GetProperty("paths").TryGetProperty("/AdminBlog", out _));
    }
}
