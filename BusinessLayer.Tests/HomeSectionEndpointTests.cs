using System.Net;
using System.Text.RegularExpressions;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer.Tests;

public sealed class HomeSectionEndpointTests
{
    [Fact]
    public async Task HomeIncludesAllSectionsAndCountsOnlyVisiblePublishedContent()
    {
        using var factory = new PlatformWebFactory();
        await factory.InitializeAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Services.AddRange(
                new Service { Id = 50, Title = "Published ritual", IsPublish = true },
                new Service { Id = 51, Title = "Hidden ritual", IsPublish = false },
                new Service { Id = 52, Title = "Deleted ritual", IsPublish = true });
            await db.SaveChangesAsync();
            db.Remove(await db.Services.FindAsync(52) ?? throw new InvalidOperationException());
            await db.SaveChangesAsync();
        }
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        foreach (var marker in new[] { "luxe-hero", "id=\"rituals\"", "id=\"home-statistics\"", "luxe-story", "luxe-blog-grid",
                     "guest-experiences-title", "luxe-gallery-caption", "luxe-booking-band" })
            Assert.Contains(marker, html);
        var statistics = Regex.Match(html, "<section[^>]*id=\"home-statistics\"[\\s\\S]*?</section>").Value;
        var values = Regex.Matches(statistics, "<dd[^>]*>([^<]+)</dd>").Select(match => match.Groups[1].Value).ToArray();
        Assert.Equal(new[] { "1", "2", "7" }, values);
        Assert.Contains("09:00", statistics);
        Assert.Contains("00:00", statistics);
        var story = Regex.Match(html, "<section[^>]*id=\"home-story\"[\\s\\S]*?</section>").Value;
        Assert.Contains("İyi Yaşamın Zarif Sanatı", story);
        Assert.Equal(2, Regex.Matches(story, "<p>").Count);
        Assert.Equal(4, Regex.Matches(story, "<li>").Count);
        foreach (var benefit in new[] { "10+ yıllık deneyim", "Sertifikalı uzman terapistler", "Doğal ve organik ürünler", "Kişiye özel program" })
            Assert.Contains(benefit, story);
        Assert.Contains("href=\"/hakkimizda\"", story);
        Assert.Contains("Hakkımızda Daha Fazla", story);
        Assert.DoesNotContain("data-aos", story);
    }
}
