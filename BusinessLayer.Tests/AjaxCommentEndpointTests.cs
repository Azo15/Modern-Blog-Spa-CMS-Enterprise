using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer.Tests;

public sealed class AjaxCommentEndpointTests
{
    [Fact]
    public async Task AsyncCreateEditDeletePreserveApprovalAndOwnership()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Comments.Add(new Comment { Id = 20, BlogId = 1, AppUserId = 6, Content = "Other customer's comment" });
            await db.SaveChangesAsync();
        }
        using var client = AjaxInteractionEndpointTests.Client(factory); await AjaxInteractionEndpointTests.Login(client);
        var token = await AjaxInteractionEndpointTests.Token(client);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        var fields = new Dictionary<string, string> { ["blogId"] = "1", ["content"] = "My async comment", ["slug"] = "approved-massage", ["__RequestVerificationToken"] = token };
        var response = await client.PostAsync("/Comment/Create", new FormUrlEncodedContent(fields)); response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<JsonElement>(); var id = data.GetProperty("comment").GetProperty("id").GetInt32();
        Assert.False(data.GetProperty("comment").GetProperty("isApproved").GetBoolean());
        using var guest = AjaxInteractionEndpointTests.Client(factory);
        Assert.DoesNotContain("My async comment", await guest.GetStringAsync("/yapilan-isler/approved-massage/yorumlar"));
        Assert.Contains("My async comment", await client.GetStringAsync("/yapilan-isler/approved-massage/yorumlar"));
        fields["id"] = "20"; // belongs to another customer
        Assert.Equal(HttpStatusCode.Forbidden, (await client.PostAsync("/Comment/Update", new FormUrlEncodedContent(fields))).StatusCode);
        fields["id"] = id.ToString(); fields["content"] = "Updated async comment";
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/Comment/Update", new FormUrlEncodedContent(fields))).StatusCode);
        Assert.Contains("Updated async comment", await client.GetStringAsync("/yapilan-isler/approved-massage/yorumlar"));
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/Comment/Delete", new FormUrlEncodedContent(fields))).StatusCode);
        Assert.DoesNotContain("Updated async comment", await client.GetStringAsync("/yapilan-isler/approved-massage/yorumlar"));
    }
    [Fact]
    public async Task ReplyIsPendingAndInvalidCommentDoesNotSucceed()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = AjaxInteractionEndpointTests.Client(factory); await AjaxInteractionEndpointTests.Login(client);
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        var fields = new Dictionary<string, string> { ["blogId"] = "1", ["parentCommentId"] = "10", ["content"] = "Reply for approval", ["slug"] = "approved-massage", ["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(client) };
        var response = await client.PostAsync("/Comment/Create", new FormUrlEncodedContent(fields)); response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(10, data.GetProperty("comment").GetProperty("parentCommentId").GetInt32());
        Assert.False(data.GetProperty("comment").GetProperty("isApproved").GetBoolean());
        fields["content"] = "";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Comment/Create", new FormUrlEncodedContent(fields))).StatusCode);
    }
}
