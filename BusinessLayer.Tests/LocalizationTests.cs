using System.Globalization;
using BusinessLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebUI.Controllers;

namespace BusinessLayer.Tests;

public sealed class LocalizationTests
{
    [Theory]
    [InlineData("tr-TR", "Ana Sayfa", "Geri yükle")]
    [InlineData("en-US", "Home", "Restore")]
    public void SharedResourcesResolveMenuAndActionsInSelectedCulture(string culture, string home, string restore)
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            using var provider = new ServiceCollection().AddLogging()
                .AddLocalization(options => options.ResourcesPath = "Resources").BuildServiceProvider();
            var text = provider.GetRequiredService<IStringLocalizer<SharedResource>>();
            Assert.Equal(home, text["Home"].Value);
            Assert.Equal(restore, text["Restore"].Value);
            Assert.False(text["ImageInvalidFormat"].ResourceNotFound);
            Assert.False(text["Contact.Success"].ResourceNotFound);
            Assert.False(text["CommentDepth"].ResourceNotFound);
        }
        finally { CultureInfo.CurrentUICulture = original; }
    }

    [Theory]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    public void LanguageChangeWritesEssentialCookieAndKeepsLocalReturnPath(string culture)
    {
        var controller = CreateController();
        var redirect = Assert.IsType<LocalRedirectResult>(controller.SetLanguage(culture, "/Blog?page=2"));
        Assert.Equal("/Blog?page=2", redirect.Url);
        var cookie = controller.Response.Headers.SetCookie.ToString();
        Assert.Contains(".AspNetCore.Culture=", cookie);
        Assert.Contains(culture, cookie);
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://attacker.example/")]
    [InlineData("//attacker.example/")]
    public void LanguageChangePreventsExternalRedirects(string returnUrl)
    {
        var controller = CreateController();
        Assert.Equal("/", Assert.IsType<LocalRedirectResult>(controller.SetLanguage("en-US", returnUrl)).Url);
    }

    [Fact]
    public void UnsupportedCultureDoesNotWriteCookie()
    {
        var controller = CreateController();
        Assert.IsType<BadRequestResult>(controller.SetLanguage("de-DE", "/"));
        Assert.Equal(0, controller.Response.Headers.SetCookie.Count);
    }

    private static HomeController CreateController()
    {
        var controller = new HomeController(NullLogger<HomeController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor()
            }
        };
        // Use the framework's URL helper so the redirect test exercises the real local URL rules.
        controller.Url = new UrlHelper(controller.ControllerContext);
        return controller;
    }
}
