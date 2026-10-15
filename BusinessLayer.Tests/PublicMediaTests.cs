using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebUI.Controllers;
using WebUI.Services;

namespace BusinessLayer.Tests;

public sealed class PublicMediaTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "xspa-media-" + Guid.NewGuid().ToString("N"));
    private readonly PublicMediaService media;

    public PublicMediaTests()
    {
        Directory.CreateDirectory(Path.Combine(root, "img"));
        Directory.CreateDirectory(Path.Combine(root, "uploads", "blogs"));
        File.WriteAllText(Path.Combine(root, "img", "blog-derindoku.webp"), "existing legacy shared file");
        File.WriteAllText(Path.Combine(root, "uploads", "blogs", "mine.webp"), "existing custom upload");
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(e => e.WebRootPath).Returns(root);
        media = new PublicMediaService(environment.Object);
    }

    [Fact]
    public void SharedExistingSeedPhotoIsReassignedToDistinctArticlePhotos()
    {
        const string shared = "/img/blog-derindoku.webp";
        var images = new[] {
            media.BlogImage("İsveç Masajı ve Faydaları", shared, 1),
            media.BlogImage("Derin Doku Masajı Kimler İçin Uygundur?", shared, 2),
            media.BlogImage("Refleksoloji: Ayak Noktalarından Şifa", shared, 3) };
        Assert.Equal(new[] { "/img/blog-isvec.webp", "/img/blog-derindoku.webp", "/img/blog-refleksoloji.webp" }, images);
        Assert.Equal(images.Length, images.Distinct().Count());
    }

    [Fact]
    public void GenuineUploadTakesPriorityOverEditorialAssignments()
    {
        const string uploaded = "~/uploads/blogs/mine.webp";
        Assert.Equal("/uploads/blogs/mine.webp", media.BlogImage("İsveç Masajı ve Faydaları", uploaded, 1));
        Assert.Equal("existing custom upload", File.ReadAllText(Path.Combine(root, "uploads", "blogs", "mine.webp")));
    }

    [Fact]
    public void MissingUnknownEntitiesGetStableIndependentArtwork()
    {
        var first = media.BlogImage("A new editorial article", null, 101);
        Assert.Equal(first, media.BlogImage("A new editorial article", null, 101));
        Assert.NotEqual(first, media.BlogImage("A new editorial article", null, 102));
        Assert.NotEqual(first, media.ServiceImage("A new editorial article", null, 101));
        Assert.StartsWith("/media/placeholder/blog/", first);
        Assert.Equal(first, media.BlogImage("A new editorial article", first, 101));
        Assert.NotEqual(media.BlogImage("İsveç Masajı ve Faydaları", null, 101),
            media.BlogImage("İsveç Masajı ve Faydaları", null, 102));
        var repeatedSeedTitle = PublicMediaService.Placeholder("blog", 102, "İsveç Masajı ve Faydaları");
        Assert.Equal(repeatedSeedTitle, media.BlogImage("İsveç Masajı ve Faydaları", repeatedSeedTitle.TrimStart('/'), 102));
    }

    [Theory]
    [InlineData("../uploads/blogs/mine.webp")]
    [InlineData("https://example.test/tracker.webp")]
    [InlineData("img/../../uploads/blogs/mine.webp")]
    public void UnsafeStoredPathsAreNeverReturned(string storedPath) =>
        Assert.StartsWith("/media/placeholder/blog/", media.BlogImage("Unknown", storedPath, 99));

    [Fact]
    public void PlaceholderEndpointOnlyRendersValidatedNumericArtwork()
    {
        var controller = new PublicMediaController();
        Assert.IsType<NotFoundResult>(controller.Placeholder("blog", "<script>alert(1)</script>"));
        Assert.IsType<NotFoundResult>(controller.Placeholder("<script>", new string('a', 24)));
        var result = Assert.IsType<ContentResult>(controller.Placeholder("blog", new string('a', 24)));
        Assert.StartsWith("image/svg+xml", result.ContentType);
        Assert.Contains("<svg", result.Content);
        Assert.DoesNotContain("<script", result.Content);
    }

    [Fact]
    public void ShippedPhotographsHaveUniqueSourcesAndUniqueFileContents()
    {
        var repository = FindRepository();
        var wwwroot = Path.Combine(repository, "WebUI", "wwwroot");
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(wwwroot, "img", "media-sources.json")));
        var assets = document.RootElement.GetProperty("assets").EnumerateArray().ToArray();
        Assert.Equal(65, assets.Length);
        Assert.Equal(assets.Length, assets.Select(a => a.GetProperty("photoId").GetInt32()).Distinct().Count());
        var hashes = assets.Select(asset => {
            var file = Path.Combine(wwwroot, asset.GetProperty("file").GetString()!.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(file), file);
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))).ToLowerInvariant();
            Assert.Equal(asset.GetProperty("sha256").GetString(), hash);
            return hash;
        }).ToArray();
        Assert.Equal(hashes.Length, hashes.Distinct().Count());
    }

    private static string FindRepository()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
                if (File.Exists(Path.Combine(directory.FullName, "WebUI", "wwwroot", "img", "media-sources.json")))
                    return directory.FullName;
        throw new DirectoryNotFoundException("The checked-in public media manifest was not found.");
    }

    public void Dispose() => Directory.Delete(root, recursive: true);
}
