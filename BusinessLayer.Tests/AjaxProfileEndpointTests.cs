using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace BusinessLayer.Tests;

public sealed class AjaxProfileEndpointTests
{
    [Fact]
    public async Task RemovalIsIdempotentAndDoesNotRemoveOtherCustomersFavorites()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using var owner = AjaxInteractionEndpointTests.Client(factory); await AjaxInteractionEndpointTests.Login(owner);
        var fields = new Dictionary<string, string> { ["blogId"] = "1", ["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(owner) };
        owner.DefaultRequestHeaders.Add("Accept", "application/json");
        (await owner.PostAsync("/BlogInteraction/Favorite", new FormUrlEncodedContent(fields))).EnsureSuccessStatusCode();
        using var other = AjaxInteractionEndpointTests.Client(factory);
        var loginFields = new Dictionary<string, string> { ["Email"] = "other-author@acceptance.test", ["Password"] = PlatformWebFactory.Password, ["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(other) };
        Assert.Equal(HttpStatusCode.Redirect, (await other.PostAsync("/dashboard/giris", new FormUrlEncodedContent(loginFields))).StatusCode);
        other.DefaultRequestHeaders.Add("Accept", "application/json");
        var otherFields = new Dictionary<string, string> { ["blogId"] = "1", ["__RequestVerificationToken"] = await AjaxInteractionEndpointTests.Token(other) };
        (await other.PostAsync("/BlogInteraction/Favorite", new FormUrlEncodedContent(otherFields))).EnsureSuccessStatusCode();
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var response = await owner.PostAsync("/Profile/RemoveFavorite", new FormUrlEncodedContent(fields)); response.EnsureSuccessStatusCode();
            var state = await response.Content.ReadFromJsonAsync<JsonElement>(); Assert.False(state.GetProperty("isFavorite").GetBoolean()); Assert.Equal(0, state.GetProperty("remainingCount").GetInt32());
        }
        Assert.Contains("Published Massage", await other.GetStringAsync("/Profile"));
        Assert.DoesNotContain("data-favorite-card", await owner.GetStringAsync("/Profile"));
    }
}
