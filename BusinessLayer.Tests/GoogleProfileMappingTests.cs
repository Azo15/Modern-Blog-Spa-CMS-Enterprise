using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace BusinessLayer.Tests;
public sealed class GoogleProfileMappingTests
{
    [Theory]
    [InlineData("{\"email\":\"guest@example.test\",\"email_verified\":true}",true)]
    [InlineData("{\"email\":\"guest@example.test\",\"email_verified\":false}",false)]
    [InlineData("{\"email\":\"guest@example.test\"}",false)]
    [InlineData("{\"email\":\"guest@example.test\",\"verified_email\":true}",false)]
    public async Task ActualGoogleConfigurationReadsV3VerifiedEmailWithoutTrustingMissingOrLegacyFlags(string profile,bool expected)
    {
        using var factory=new PlatformWebFactory(googleEnabled:true);await factory.InitializeAsync();
        var options=factory.Services.GetRequiredService<IOptionsMonitor<GoogleOptions>>().Get("Google");
        Assert.EndsWith("/oauth2/v3/userinfo",options.UserInformationEndpoint);
        using var json=JsonDocument.Parse(profile);var identity=new ClaimsIdentity("Google");
        foreach(var action in options.ClaimActions)action.Run(json.RootElement,identity,"Google");
        Assert.Equal("guest@example.test",identity.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal(expected,string.Equals(identity.FindFirst("urn:xspa:email_verified")?.Value,"true",StringComparison.OrdinalIgnoreCase));
    }
}
