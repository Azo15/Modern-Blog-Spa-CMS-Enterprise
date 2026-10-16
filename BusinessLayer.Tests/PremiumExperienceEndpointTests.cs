using System.Net;
using System.Text.RegularExpressions;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer.Tests;

public sealed class PremiumExperienceEndpointTests
{
    [Fact]
    public async Task AboutPageContainsFourFeaturesAndSeparateBookingInvitation()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using var client = factory.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/hakkimizda"));
        Assert.Equal(4, Regex.Matches(html, "class=\"luxe-about-feature\"").Count);
        Assert.Contains("Kişiye Özel Program", html);
        Assert.Contains("Gizlilik & Konfor", html);
        Assert.Contains("Her misafir için sıfırdan tasarlanan ritüel.", html);
        Assert.Contains("Yalnızca size ayrılmış özel alanlar.", html);
        Assert.Contains("href=\"/randevu\"", html);
    }

    [Fact]
    public async Task BlogStatisticsIncludeUncategorizedPublishedArticlesEvenWhenFilteredPageIsEmpty()
    {
        using var factory = new PlatformWebFactory(); await factory.InitializeAsync();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Blogs.Add(new Blog { Title = "Uncategorized story", Slug = "uncategorized", AppUserId = 3, PublishStatus = PublishStatus.Published });
            await db.SaveChangesAsync();
        }
        using var client = factory.CreateClient();
        var html = WebUtility.HtmlDecode(await client.GetStringAsync("/yapilan-isler?categoryId=2"));
        Assert.Equal("3", Regex.Match(html, "<b data-blog-total[^>]*>([0-9]+)</b>").Groups[1].Value);
        Assert.Equal("1", Regex.Match(html, "<b data-category-total[^>]*>([0-9]+)</b>").Groups[1].Value);
        Assert.Contains("id=\"blog-search\"", html);
        Assert.Contains("Sonuç bulunamadı", html);
    }
}
