using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BusinessLayer.Tests;

public sealed class AjaxInteractionEndpointTests
{
    internal static HttpClient Client(PlatformWebFactory factory) => factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    internal static async Task<string> Token(HttpClient client, string path = "/dashboard/giris") => WebUtility.HtmlDecode(Regex.Match(await client.GetStringAsync(path), "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    internal static async Task Login(HttpClient client) => Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/dashboard/giris", new FormUrlEncodedContent(new Dictionary<string, string> { ["Email"] = "user@acceptance.test", ["Password"] = PlatformWebFactory.Password, ["__RequestVerificationToken"] = await Token(client) }))).StatusCode);

    [Theory]
    [InlineData("Accept", "application/json")]
    [InlineData("X-Requested-With", "XMLHttpRequest")]
    public async Task FavoriteAndReactionReturnAuthoritativeCountsAndRequireCsrf(string header, string value)
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync(); using var client = Client(factory); await Login(client);
        client.DefaultRequestHeaders.Add(header, value);
        var fields = new Dictionary<string, string> { ["blogId"] = "1", ["slug"] = "approved-massage" };
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/BlogInteraction/Favorite", new FormUrlEncodedContent(fields))).StatusCode);
        fields["__RequestVerificationToken"] = await Token(client);
        var favorite = await client.PostAsync("/BlogInteraction/Favorite", new FormUrlEncodedContent(fields)); favorite.EnsureSuccessStatusCode();
        var state = await favorite.Content.ReadFromJsonAsync<JsonElement>(); Assert.True(state.GetProperty("isFavorite").GetBoolean()); Assert.Equal(1, state.GetProperty("favoriteCount").GetInt32());
        fields["reactionType"] = "2";
        var reaction = await client.PostAsync("/BlogInteraction/Reaction", new FormUrlEncodedContent(fields)); reaction.EnsureSuccessStatusCode();
        state = await reaction.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(1, state.GetProperty("heartCount").GetInt32()); Assert.Equal(2, state.GetProperty("reaction").GetInt32());
        fields["reactionType"] = "";
        reaction = await client.PostAsync("/BlogInteraction/Reaction", new FormUrlEncodedContent(fields)); reaction.EnsureSuccessStatusCode();
        state = await reaction.Content.ReadFromJsonAsync<JsonElement>(); Assert.Equal(0, state.GetProperty("heartCount").GetInt32());
    }
}
