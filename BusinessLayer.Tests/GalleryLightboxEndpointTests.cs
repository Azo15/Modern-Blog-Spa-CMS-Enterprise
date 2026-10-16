using System.Net;
using System.Text.RegularExpressions;
namespace BusinessLayer.Tests;
public sealed class GalleryLightboxEndpointTests
{
    [Fact]
    public async Task GalleryLoadsLocalLightboxAssetsAndLinksFullImagesWithCategories()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/gorsel-galerisi");
        Assert.Contains("/lib/fancybox/js/fancybox.umd.js", html);
        Assert.Contains("/lib/fancybox/css/fancybox.min.css", html);
        var links = Regex.Matches(html, "<a[^>]*class=\"luxe-gallery-item\"[^>]*>");
        Assert.Equal(8, links.Count);
        foreach (Match link in links)
        {
            Assert.Contains("data-fancybox=\"spa-gallery\"", link.Value);
            Assert.Matches("data-category=\"(mekan|ritueller|urunler|ekip)\"", link.Value);
            var path = Regex.Match(link.Value, "href=\"([^\"]+)\"").Groups[1].Value;
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(path)).StatusCode);
        }
    }
}
