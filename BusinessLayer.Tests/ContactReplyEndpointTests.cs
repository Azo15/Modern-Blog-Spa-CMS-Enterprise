using System.Net;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer.Tests;
public sealed class ContactReplyEndpointTests
{
    private static async Task<int> Message(PlatformWebFactory f)
    {
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var message = new ContactForm { Email = "guest@example.test", FullName = "Guest", Subject = "Question", Message = "Hello" };
        db.ContactForms.Add(message); await db.SaveChangesAsync(); return message.Id;
    }
    private static async Task Login(HttpClient c, string email)
    {
        Assert.Equal(HttpStatusCode.Redirect, (await c.PostAsync("/dashboard/giris", new FormUrlEncodedContent(new Dictionary<string, string> {
            ["Email"] = email, ["Password"] = PlatformWebFactory.Password, ["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(c) }))).StatusCode);
    }
    [Theory]
    [InlineData("admin@acceptance.test")]
    [InlineData("editor@acceptance.test")]
    public async Task ManagersReplyAsyncWithHistoryCsrfAndServerOwnedRecipient(string email)
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); var id = await Message(f);
        using var c = AjaxInteractionEndpointTests.Client(f); await Login(c, email);
        Assert.Contains("data-message-reply", await c.GetStringAsync($"/AdminMessage/Details/{id}"));
        c.DefaultRequestHeaders.Add("Accept", "application/json");
        var fields = new Dictionary<string, string> { ["id"] = id.ToString(), ["replyMessage"] = "<script>unsafe()</script> Plain reply", ["clientRequestId"] = Guid.NewGuid().ToString(), ["recipientEmail"] = "attacker@example.test" };
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsync("/AdminMessage/Reply", new FormUrlEncodedContent(fields))).StatusCode);
        fields["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(c);
        var response = await c.PostAsync("/AdminMessage/Reply", new FormUrlEncodedContent(fields)); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsync("/AdminMessage/Reply", new FormUrlEncodedContent(fields))).StatusCode);
        var history = WebUtility.HtmlDecode(await c.GetStringAsync($"/AdminMessage/ReplyHistory/{id}")); Assert.Contains("Önceki yanıtlar", history);
        var encoded = await c.GetStringAsync($"/AdminMessage/ReplyHistory/{id}"); Assert.DoesNotContain("<script>unsafe()", encoded); Assert.Contains("&lt;script&gt;", encoded);
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reply = Assert.Single(await db.ContactReplies.ToListAsync()); Assert.Equal("guest@example.test", reply.RecipientEmail);
        Assert.Contains("Yanıtlandı", WebUtility.HtmlDecode(await c.GetStringAsync("/AdminMessage")));
    }
    [Fact]
    public async Task CustomerCannotReadHistoryOrReplyAndInvalidDraftIsNotPersisted()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); var id = await Message(f);
        using var c = AjaxInteractionEndpointTests.Client(f); await Login(c, "user@acceptance.test"); c.DefaultRequestHeaders.Add("Accept", "application/json");
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync($"/AdminMessage/ReplyHistory/{id}")).StatusCode);
        var fields = new Dictionary<string, string> { ["id"] = id.ToString(), ["replyMessage"] = "Private answer", ["clientRequestId"] = Guid.NewGuid().ToString(), ["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(c) };
        Assert.Equal(HttpStatusCode.Forbidden, (await c.PostAsync("/AdminMessage/Reply", new FormUrlEncodedContent(fields))).StatusCode);
        using var admin = AjaxInteractionEndpointTests.Client(f); await Login(admin, "admin@acceptance.test"); admin.DefaultRequestHeaders.Add("Accept", "application/json");
        fields["replyMessage"] = ""; fields["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(admin);
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync("/AdminMessage/Reply", new FormUrlEncodedContent(fields))).StatusCode);
        using var scope = f.Services.CreateScope(); Assert.Empty(await scope.ServiceProvider.GetRequiredService<AppDbContext>().ContactReplies.ToListAsync());
    }
    [Fact]
    public async Task StaffCanSeeVisitorChartsAndCustomerCannotSeeDashboard()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var c = AjaxInteractionEndpointTests.Client(f);
        await Login(c, "editor@acceptance.test"); var page = WebUtility.HtmlDecode(await c.GetStringAsync("/AdminDashboard"));
        Assert.Contains("visitor-trend-data", page); Assert.Contains("Dijital misafir deneyimi", page); Assert.Contains("Mesaj yanıtlama oranı", page);
        using var customer = AjaxInteractionEndpointTests.Client(f); await Login(customer, "user@acceptance.test");
        Assert.Equal(HttpStatusCode.Forbidden, (await customer.GetAsync("/AdminDashboard")).StatusCode);
    }
}
