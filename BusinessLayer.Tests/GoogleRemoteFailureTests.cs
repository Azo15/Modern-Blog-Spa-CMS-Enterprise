using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using WebUI.Infrastructure;
namespace BusinessLayer.Tests;
public sealed class GoogleRemoteFailureTests
{
    [Theory]
    [InlineData("invalid_client; secret=private-client-secret")]
    [InlineData("Correlation failed; code=private-authorization-code")]
    public async Task RemoteFailureReturnsToSignInWithoutLeakingProviderDetailsOrAuthenticating(string detail)
    {
        using var services=new ServiceCollection().AddLogging()
            .AddSingleton(Mock.Of<ITempDataProvider>())
            .AddSingleton<ITempDataDictionaryFactory,TempDataDictionaryFactory>().BuildServiceProvider();
        var http=new DefaultHttpContext { RequestServices=services };
        var failure=new RemoteFailureContext(http,new AuthenticationScheme("Google","Google",typeof(GoogleHandler)),new GoogleOptions(),new AuthenticationFailureException(detail));
        await GoogleLoginFailureHandler.HandleAsync(failure);
        Assert.Equal(StatusCodes.Status302Found,http.Response.StatusCode);
        Assert.Equal("/dashboard/giris",http.Response.Headers.Location.ToString());
        var data=services.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(http);
        var message=Assert.IsType<string>(data["ErrorMessage"]);
        Assert.Contains("Google",message);Assert.DoesNotContain("private-",message);
        Assert.DoesNotContain("invalid_client",message);
        Assert.False(http.User.Identity?.IsAuthenticated ?? false);
        Assert.True(failure.Result?.Handled);
    }
}
