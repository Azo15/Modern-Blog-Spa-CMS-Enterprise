using System.Net;
using System.Net.Http.Json;
using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace BusinessLayer.Tests;
public sealed class NotificationEndpointTests
{
    private static async Task Login(HttpClient c,string email)
    {
        var response = await c.PostAsync("/dashboard/giris",new FormUrlEncodedContent(new Dictionary<string,string> { ["Email"]=email,["Password"]=PlatformWebFactory.Password,["__RequestVerificationToken"]=await AjaxInteractionEndpointTests.Token(c) }));
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode); c.DefaultRequestHeaders.Add("Accept","application/json");
    }
    [Theory]
    [InlineData("admin@acceptance.test",1,2)]
    [InlineData("editor@acceptance.test",2,1)]
    public async Task ManagersSeeOnlyTheirNotificationsAndMutationsRequireCsrf(string email,int userId,int otherId)
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var c = AjaxInteractionEndpointTests.Client(f); await Login(c,email);
        using var scope = f.Services.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await service.PublishAsync(new("test:web","<script>alert(1)</script>","<img src=x onerror=alert(1)>",NotificationType.SystemFailure));
        var page = (await c.GetFromJsonAsync<NotificationPage>("/admin/notifications/list"))!; Assert.Equal(1,page.UnreadCount);
        var own = Assert.Single(page.Items); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(userId,(await db.Notifications.SingleAsync(n => n.Id == own.Id)).RecipientUserId);
        var other = await db.Notifications.SingleAsync(n => n.RecipientUserId == otherId);
        Assert.Equal(HttpStatusCode.BadRequest,(await c.PostAsync("/admin/notifications/read",new FormUrlEncodedContent(new Dictionary<string,string> { ["id"]=own.Id.ToString() }))).StatusCode);
        var token = await AjaxInteractionEndpointTests.Token(c);
        Assert.Equal(HttpStatusCode.NotFound,(await c.PostAsync("/admin/notifications/read",new FormUrlEncodedContent(new Dictionary<string,string> { ["id"]=other.Id.ToString(),["__RequestVerificationToken"]=token }))).StatusCode);
        var html = await c.GetStringAsync("/admin/notifications"); Assert.Contains("data-notification-count",html); Assert.Contains("data-notification-list",html); Assert.DoesNotContain("<script>alert(1)</script>",html); Assert.Contains("&lt;script&gt;",html);
        Assert.Equal(HttpStatusCode.OK,(await c.PostAsync("/admin/notifications/read-all",new FormUrlEncodedContent(new Dictionary<string,string> { ["throughId"]=page.LatestId.ToString(),["__RequestVerificationToken"]=token }))).StatusCode);
        Assert.Equal(0,await service.CountAsync(userId)); Assert.Equal(1,await service.CountAsync(otherId));
        Assert.Equal(0,(await c.GetFromJsonAsync<NotificationPage>("/admin/notifications/list"))!.UnreadCount);
    }
    [Fact]
    public async Task CustomersAnonymousAndRoleRevokedUsersCannotAccessNotificationEndpoints()
    {
        using var f = new PlatformWebFactory(); await f.InitializeAsync(); using var anonymous = AjaxInteractionEndpointTests.Client(f); anonymous.DefaultRequestHeaders.Add("Accept","application/json");
        Assert.Equal(HttpStatusCode.Unauthorized,(await anonymous.GetAsync("/admin/notifications/list")).StatusCode);
        using var customer = AjaxInteractionEndpointTests.Client(f); await Login(customer,"user@acceptance.test"); Assert.Equal(HttpStatusCode.Forbidden,(await customer.GetAsync("/admin/notifications/count")).StatusCode);
        using var admin = AjaxInteractionEndpointTests.Client(f); await Login(admin,"admin@acceptance.test");
        using var scope = f.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); await db.UserRoles.Where(x => x.UserId == 1).ExecuteDeleteAsync();
        Assert.Equal(HttpStatusCode.Forbidden,(await admin.GetAsync("/admin/notifications/list")).StatusCode);
    }
}
