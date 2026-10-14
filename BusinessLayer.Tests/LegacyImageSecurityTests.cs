using System.Diagnostics;
using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using FluentValidation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using SkiaSharp;
using WebUI.Services;

namespace BusinessLayer.Tests;

public sealed class LegacyImageSecurityTests
{
    private sealed class Fixture : IDisposable
    {
        public readonly string OwnedRoot = Path.Combine(Path.GetTempPath(), "modern-blog-path-tests-" + Guid.NewGuid().ToString("N"));
        public string WebRoot => Path.Combine(OwnedRoot, "wwwroot");
        public string Outside => Path.Combine(OwnedRoot, "outside");
        private readonly ServiceProvider provider = new ServiceCollection().AddLogging().AddLocalization(o => o.ResourcesPath = "Resources").BuildServiceProvider();
        public ImageOptimizationService Images { get; }
        public FileHelper Files { get; }
        public Fixture()
        {
            Directory.CreateDirectory(WebRoot); Directory.CreateDirectory(Outside);
            var environment = Mock.Of<IWebHostEnvironment>(e => e.WebRootPath == WebRoot);
            Images = new ImageOptimizationService(environment, provider.GetRequiredService<IStringLocalizer<BusinessLayer.SharedResource>>());
            Files = new FileHelper(environment, Images);
        }
        public void Dispose()
        {
            provider.Dispose();
            var path = Path.GetFullPath(OwnedRoot);
            if (!path.StartsWith(Path.GetFullPath(Path.GetTempPath()), StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Unsafe test cleanup path.");
            Directory.Delete(path, true);
        }
    }

    private static IFormFile Upload(byte[] bytes, string filename = "photo.png", string mime = "image/png")
        => new FormFile(new MemoryStream(bytes), 0, bytes.Length, "ImageFile", filename) { Headers = new HeaderDictionary(), ContentType = mime };
    private static byte[] Image()
    {
        using var bitmap = new SKBitmap(12, 6); bitmap.Erase(SKColors.Teal);
        using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }

    [Fact]
    public async Task LegacyScreensUseValidatedWebpAndReplacementRetainsHistoricalImage()
    {
        using var fixture = new Fixture();
        var oldPath = Path.Combine(fixture.WebRoot, "legacy.jpg"); await File.WriteAllTextAsync(oldPath, "restorable original");
        var relative = await fixture.Files.UpdateAsync(Upload(Image()), Path.Combine(fixture.WebRoot, "about"), "legacy.jpg");
        Assert.StartsWith("uploads/blogs/", relative);
        using var codec = SKCodec.Create(File.OpenRead(Path.Combine(fixture.WebRoot, relative!)));
        Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
        Assert.Equal("restorable original", await File.ReadAllTextAsync(oldPath));
        Assert.False(Directory.Exists(Path.Combine(fixture.WebRoot, "about")));
    }

    [Fact]
    public async Task FailedLegacyReplacementPreservesTheOriginalAndWritesNoPayload()
    {
        using var fixture = new Fixture();
        var oldPath = Path.Combine(fixture.WebRoot, "legacy.jpg"); await File.WriteAllTextAsync(oldPath, "original");
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Files.UpdateAsync(Upload("<script>alert(1)</script>"u8.ToArray(), "payload.exe", "image/png"), fixture.WebRoot, "legacy.jpg"));
        Assert.Equal("original", await File.ReadAllTextAsync(oldPath));
        Assert.False(Directory.Exists(Path.Combine(fixture.WebRoot, "uploads")));
    }

    [Fact]
    public async Task UploadRejectsOutsideAndPrefixLookalikeDirectoriesBeforeReadingPayload()
    {
        using var fixture = new Fixture();
        foreach (var unsafePath in new[] { fixture.Outside, fixture.WebRoot + "-lookalike", Path.Combine(fixture.WebRoot, "..", "outside") })
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Files.UploadAsync(Upload(Image()), unsafePath));
        Assert.Empty(Directory.GetFileSystemEntries(fixture.Outside));
        Assert.False(Directory.Exists(Path.Combine(fixture.WebRoot, "uploads")));
    }

    [Theory]
    [InlineData("../outside/keep.txt")]
    [InlineData("..\\outside\\keep.txt")]
    [InlineData("images/../../outside/keep.txt")]
    [InlineData("/")]
    [InlineData("C:\\outside\\keep.txt")]
    [InlineData("images/keep.txt:payload")]
    public async Task DeleteRejectsTraversalRootAbsoluteAndAlternateDataStreamPaths(string relative)
    {
        using var fixture = new Fixture();
        var outside = Path.Combine(fixture.Outside, "keep.txt"); await File.WriteAllTextAsync(outside, "protected");
        Assert.Throws<UnauthorizedAccessException>(() => fixture.Files.Delete(relative));
        Assert.Equal("protected", await File.ReadAllTextAsync(outside));
    }

    [Fact]
    public async Task DeleteRemovesOnlyTheSelectedLocalFileAndMissingPathsAreHarmless()
    {
        using var fixture = new Fixture();
        var directory = Path.Combine(fixture.WebRoot, "images"); Directory.CreateDirectory(directory);
        var selected = Path.Combine(directory, "selected.webp"); var kept = Path.Combine(directory, "keep.webp");
        await File.WriteAllTextAsync(selected, "selected"); await File.WriteAllTextAsync(kept, "keep");
        fixture.Files.Delete("/images/selected.webp"); fixture.Files.Delete("images/missing.webp"); fixture.Files.Delete("");
        Assert.False(File.Exists(selected)); Assert.True(File.Exists(kept));
    }

    [Fact]
    public async Task LinkedUploadDirectoriesCannotWriteOrDeleteOutsideStorage()
    {
        using var fixture = new Fixture();
        var target = Path.Combine(fixture.Outside, "keep.webp"); await File.WriteAllTextAsync(target, "protected");
        var link = Path.Combine(fixture.WebRoot, "uploads");
        CreateDirectoryLink(link, fixture.Outside);
        try
        {
            Assert.Throws<UnauthorizedAccessException>(() => fixture.Files.Delete("uploads/keep.webp"));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Files.UploadAsync(Upload(Image()), fixture.WebRoot));
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Images.StoreAsync(Upload(Image()), "avatars"));
            Assert.Equal("protected", await File.ReadAllTextAsync(target));
            Assert.Equal(new[] { target }, Directory.GetFiles(fixture.Outside));
            Assert.False(Directory.Exists(Path.Combine(fixture.Outside, "avatars")));
        }
        finally { Directory.Delete(link); }
    }

    private static void CreateDirectoryLink(string link, string target)
    {
        if (!OperatingSystem.IsWindows()) { Directory.CreateSymbolicLink(link, target); return; }
        // Windows directory junctions work without Developer Mode or symlink privileges.
        var info = new ProcessStartInfo("cmd.exe") { CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "/c", "mklink", "/J", link, target }) info.ArgumentList.Add(argument);
        using var process = Process.Start(info)!;
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        process.WaitForExit(); Assert.True(process.ExitCode == 0, output);
    }
}
