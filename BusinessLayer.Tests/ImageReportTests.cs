using System.Globalization;
using System.Reflection;
using BusinessLayer.Concrete;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using ClosedXML.Excel;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using PdfSharp.Pdf.IO;
using SkiaSharp;
using WebUI.Controllers;
using WebUI.Services;

namespace BusinessLayer.Tests;

public sealed class ImageReportTests
{
    private sealed class ImageFixture : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "modern-blog-image-tests", Guid.NewGuid().ToString("N"));
        private readonly ServiceProvider provider = new ServiceCollection().AddLogging().AddLocalization(options => options.ResourcesPath = "Resources").BuildServiceProvider();
        public ImageOptimizationService Service { get; }
        public ImageFixture()
        {
            var environment = new Mock<IWebHostEnvironment>(); environment.SetupGet(e => e.WebRootPath).Returns(Root);
            Service = new ImageOptimizationService(environment.Object, provider.GetRequiredService<IStringLocalizer<BusinessLayer.SharedResource>>());
        }
        public string PathFor(string filename, string folder = "blogs") => Path.Combine(Root, "uploads", folder, filename);
        public void Dispose() { provider.Dispose(); if (Directory.Exists(Root)) Directory.Delete(Root, true); }
    }

    private static byte[] Png(int width = 2400, int height = 1000)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColors.Teal);
        using var paint = new SKPaint { Color = SKColors.White }; canvas.DrawCircle(width / 2f, height / 2f, height / 3f, paint);
        using var image = SKImage.FromBitmap(bitmap); using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }

    private static IFormFile Upload(byte[] data, string filename = "photo.png", string mime = "image/png", long? reportedLength = null)
    {
        var file = new Mock<IFormFile>(); file.SetupGet(f => f.Length).Returns(reportedLength ?? data.Length);
        file.SetupGet(f => f.FileName).Returns(filename); file.SetupGet(f => f.ContentType).Returns(mime);
        file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(data, false)); return file.Object;
    }

    [Fact]
    public async Task ImageBecomesSmallerWebpWithProportionalMaximumWidthAndLegacyImageSurvives()
    {
        using var f = new ImageFixture(); var source = Png();
        Directory.CreateDirectory(Path.Combine(f.Root, "uploads", "blogs"));
        var legacy = f.PathFor("legacy.jpg"); await File.WriteAllBytesAsync(legacy, [1, 2, 3]);
        var filename = await f.Service.StoreAsync(Upload(source), "blogs");
        Assert.Matches("^[a-f0-9]{32}\\.webp$", filename!);
        var result = await File.ReadAllBytesAsync(f.PathFor(filename!));
        using var codec = SKCodec.Create(new MemoryStream(result));
        Assert.NotNull(codec); Assert.Equal(SKEncodedImageFormat.Webp, codec.EncodedFormat);
        Assert.Equal(1200, codec.Info.Width); Assert.Equal(500, codec.Info.Height);
        Assert.True(result.Length < source.Length); Assert.Equal(new byte[] { 1, 2, 3 }, await File.ReadAllBytesAsync(legacy));
    }

    [Fact]
    public async Task AvatarUsesSamePipelineWithoutUpscalingAndMetadataFilenameIsDiscarded()
    {
        using var f = new ImageFixture();
        var filename = await f.Service.StoreAsync(Upload(Png(80, 40), "../../profile.png"), "avatars");
        using var codec = SKCodec.Create(File.OpenRead(f.PathFor(filename!, "avatars")));
        Assert.Equal(80, codec.Info.Width); Assert.Equal(40, codec.Info.Height);
        Assert.True(File.Exists(f.PathFor(filename!, "avatars")));
    }

    [Theory]
    [InlineData("photo.png", "image/png", "<script>alert(1)</script>")]
    [InlineData("file.exe", "image/png", "MZ executable")]
    [InlineData("graphic.svg", "image/svg+xml", "<svg></svg>")]
    [InlineData("photo.png", "image/png", "GIF89a")]
    public async Task ScriptExecutableSvgAndTruncatedImageAreRejected(string filename, string mime, string content)
    {
        using var f = new ImageFixture();
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.StoreAsync(Upload(System.Text.Encoding.UTF8.GetBytes(content), filename, mime), "blogs"));
        Assert.False(Directory.Exists(f.Root));
    }

    [Theory]
    [InlineData("photo.jpg", "image/jpeg")]
    [InlineData("photo.png", "image/jpeg")]
    [InlineData("photo.exe", "image/png")]
    [InlineData("photo.png", "application/octet-stream")]
    public async Task SignatureMustAgreeWithMimeAndExtension(string filename, string mime)
    {
        using var f = new ImageFixture();
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.StoreAsync(Upload(Png(10, 10), filename, mime), "blogs"));
    }

    [Fact]
    public async Task CorrectPngSignatureWithoutDecodableImageIsRejected()
    {
        using var f = new ImageFixture();
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.StoreAsync(Upload([137, 80, 78, 71, 13, 10, 26, 10]), "blogs"));
        Assert.False(Directory.Exists(f.Root));
    }

    [Fact]
    public async Task FiveMbLimitIsEnforcedAgainstBothMetadataAndActualStream()
    {
        using var f = new ImageFixture();
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.StoreAsync(Upload(Png(10, 10), reportedLength: ImageOptimizationService.MaximumFileBytes + 1), "blogs"));
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.StoreAsync(Upload(new byte[ImageOptimizationService.MaximumFileBytes + 1], reportedLength: 100), "blogs"));
        Assert.False(Directory.Exists(f.Root));
    }

    [Fact]
    public async Task DecodedDimensionLimitRejectsVeryWideImagesBeforeResize()
    {
        using var f = new ImageFixture();
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.StoreAsync(Upload(Png(12001, 1)), "blogs"));
    }

    [Fact]
    public async Task ImageFolderIsAllowlistedAndEmptyOrCancelledUploadsNeverWriteFiles()
    {
        using var f = new ImageFixture();
        await Assert.ThrowsAsync<ArgumentException>(() => f.Service.StoreAsync(Upload(Png(10, 10)), "../../"));
        Assert.Null(await f.Service.StoreAsync(null, "blogs"));
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.StoreAsync(Upload([]), "blogs"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => f.Service.StoreAsync(Upload(Png(10, 10)), "blogs", new CancellationToken(true)));
        Assert.False(Directory.Exists(f.Root));
    }

    private sealed class ReportFixture : IDisposable
    {
        private readonly SqliteConnection connection = new("Data Source=:memory:");
        private readonly ServiceProvider authorizationProvider = AuthorizationTests.Provider();
        private readonly ServiceProvider localizerProvider = new ServiceCollection().AddLogging().AddLocalization(options => options.ResourcesPath = "Resources").BuildServiceProvider();
        public AppDbContext Db { get; }
        public Stage2Fixture.TestUser User { get; } = new();
        public ReportService Service { get; }
        public ReportFixture()
        {
            connection.Open(); Db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options); Db.Database.EnsureCreated();
            Service = new ReportService(Db, User, authorizationProvider.GetRequiredService<IAuthorizationService>(), localizerProvider.GetRequiredService<IStringLocalizer<BusinessLayer.SharedResource>>());
            Db.Users.AddRange(new AppUser { Id = 1, UserName = "writer", FirstName = "Çağrı", LastName = "Şahin", Email = "writer@example.test" },
                new AppUser { Id = 2, UserName = "member", FirstName = "İsmail", LastName = "Öztürk", Email = "member@example.test" });
            Db.Catagories.Add(new Catagory { Id = 1, Title = "Türkçe" });
            Db.Blogs.AddRange(new Blog { Id = 1, AppUserId = 1, CatagoryId = 1, Title = "Türkçe ç ğ ı ö ş ü İ", PublishStatus = PublishStatus.Published },
                new Blog { Id = 2, AppUserId = 1, CatagoryId = 1, Title = "Second blog", PublishStatus = PublishStatus.Published });
            Db.ContactForms.AddRange(new ContactForm { Id = 1, FullName = "Çağrı Şahin", Email = "contact@example.test", Subject = "Türkçe ç ğ ı ö ş ü İ", Message = "Private content" },
                new ContactForm { Id = 2, FullName = "=HYPERLINK(\"https://example.test\")", Email = "second@example.test", Subject = "Second contact" });
            Db.SaveChanges(); Db.ChangeTracker.Clear();
            Db.ContactForms.Where(c => c.Id == 1).ExecuteUpdate(s => s.SetProperty(c => c.CreatedDate, new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc)));
            Db.ContactForms.Where(c => c.Id == 2).ExecuteUpdate(s => s.SetProperty(c => c.CreatedDate, new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc)));
            As(AppRoles.Admin);
        }
        public void As(string? role) => User.User = AuthorizationTests.Principal(role, authenticated: role != null);
        public void Dispose() { Db.Dispose(); connection.Dispose(); authorizationProvider.Dispose(); localizerProvider.Dispose(); }
    }

    [Theory]
    [InlineData(AppRoles.Admin, true, true)]
    [InlineData(AppRoles.Staff, true, false)]
    [InlineData(AppRoles.Staff, true, false)]
    [InlineData(AppRoles.Staff, false, false)]
    [InlineData(AppRoles.Customer, false, false)]
    [InlineData(null, false, false)]
    public async Task ReportServiceEnforcesRoleMatrixBeforeDataExport(string? role, bool contacts, bool system)
    {
        using var f = new ReportFixture(); f.As(role);
        if (contacts) Assert.Equal(2, (await f.Service.GenerateAsync(ReportKind.Contacts, ReportFormat.Excel, new())).RowCount);
        else await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.GenerateAsync(ReportKind.Contacts, ReportFormat.Excel, new()));
        if (system) Assert.Equal(2, (await f.Service.GenerateAsync(ReportKind.Users, ReportFormat.Excel, new())).RowCount);
        else await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.GenerateAsync(ReportKind.Users, ReportFormat.Excel, new()));
    }

    [Fact]
    public async Task ContactDateRangeUsesInclusiveStartExclusiveEndAndSoftDeletedRowsStayHidden()
    {
        using var f = new ReportFixture();
        var start = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc); var end = start.AddDays(1);
        var report = await f.Service.GenerateAsync(ReportKind.Contacts, ReportFormat.Excel, new(start, end));
        Assert.Equal(1, report.RowCount);
        using var workbook = new XLWorkbook(new MemoryStream(report.Content)); var sheet = workbook.Worksheet(1);
        Assert.Equal("Çağrı Şahin", sheet.Cell(5, 2).GetString()); Assert.Equal(XLDataType.DateTime, sheet.Cell(5, 6).DataType);
        await f.Db.ContactForms.Where(c => c.Id == 1).ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true));
        Assert.Equal(0, (await f.Service.GenerateAsync(ReportKind.Contacts, ReportFormat.Excel, new(start, end))).RowCount);
    }

    [Fact]
    public async Task ExcelHasFiltersFreezePanesAndFormulaLikeUserStringsRemainText()
    {
        using var f = new ReportFixture(); var report = await f.Service.GenerateAsync(ReportKind.Contacts, ReportFormat.Excel, new());
        using var workbook = new XLWorkbook(new MemoryStream(report.Content)); var sheet = workbook.Worksheet(1);
        Assert.True(sheet.Table("ReportData").ShowAutoFilter); Assert.Equal(4, sheet.SheetView.SplitRow);
        var cell = sheet.Cell(5, 2); Assert.StartsWith("=HYPERLINK", cell.GetString()); Assert.False(cell.HasFormula);
        Assert.True(cell.Style.IncludeQuotePrefix); Assert.InRange(sheet.Column(2).Width, 12, 60);
    }

    [Theory]
    [InlineData("tr-TR", "Rapor", "Başlık", "Yayında")]
    [InlineData("en-US", "Report", "Title", "Published")]
    public async Task ReportHeadersAndPublicationStatesUseRequestedUiCulture(string culture, string sheetName, string titleColumn, string published)
    {
        var before = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
            using var f = new ReportFixture(); var report = await f.Service.GenerateAsync(ReportKind.Blogs, ReportFormat.Excel, new(BlogId: 1));
            Assert.Equal(1, report.RowCount); using var workbook = new XLWorkbook(new MemoryStream(report.Content));
            var sheet = workbook.Worksheet(sheetName); Assert.Equal(titleColumn, sheet.Cell(4, 2).GetString()); Assert.Equal(published, sheet.Cell(5, 4).GetString());
        }
        finally { CultureInfo.CurrentUICulture = before; }
    }

    [Fact]
    public async Task InteractionReportAppliesUserBlogDateFiltersAndExcludesTrashedBlogs()
    {
        using var f = new ReportFixture();
        f.Db.AddRange(new BlogFavorite { AppUserId = 2, BlogId = 1 }, new BlogFavorite { AppUserId = 1, BlogId = 1 },
            new BlogReaction { AppUserId = 2, BlogId = 1, ReactionType = ReactionType.Heart }, new BlogFavorite { AppUserId = 2, BlogId = 2 }); await f.Db.SaveChangesAsync();
        var result = await f.Service.GenerateAsync(ReportKind.Interactions, ReportFormat.Excel, new(UserId: 2, BlogId: 1));
        Assert.Equal(2, result.RowCount);
        using var book = new XLWorkbook(new MemoryStream(result.Content)); Assert.All(new[] { 5, 6 }, row => Assert.Equal(2, book.Worksheet(1).Cell(row, 1).GetValue<int>()));
        await f.Db.Blogs.Where(b => b.Id == 1).ExecuteUpdateAsync(s => s.SetProperty(b => b.IsDeleted, true));
        Assert.Equal(0, (await f.Service.GenerateAsync(ReportKind.Interactions, ReportFormat.Excel, new(UserId: 2, BlogId: 1))).RowCount);
    }

    [Fact]
    public async Task InvalidReportFiltersAreRejectedBeforeExport()
    {
        using var f = new ReportFixture();
        foreach (var filter in new[] { new ReportFilter(UserId: 0), new ReportFilter(BlogId: -1), new ReportFilter(DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)), new ReportFilter(DateTime.Now) })
            await Assert.ThrowsAsync<ValidationException>(() => f.Service.GenerateAsync(ReportKind.Blogs, ReportFormat.Excel, filter));
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.GenerateAsync(ReportKind.Contacts, ReportFormat.Excel, new(UserId: 1)));
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.GenerateAsync(ReportKind.Users, ReportFormat.Excel, new(BlogId: 1)));
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.GenerateAsync((ReportKind)99, ReportFormat.Excel, new()));
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.GenerateAsync(ReportKind.Blogs, (ReportFormat)99, new()));
    }

    [Fact]
    public async Task ReportRowLimitAndExcelCellLimitProduceValidationErrors()
    {
        using var f = new ReportFixture();
        f.Db.ContactForms.AddRange(Enumerable.Range(3, ReportService.MaximumRows - 1).Select(id => new ContactForm { Id = id, FullName = "Test" }));
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.GenerateAsync(ReportKind.Contacts, ReportFormat.Excel, new()));
        await f.Db.ContactForms.Where(c => c.Id == 1).ExecuteUpdateAsync(s => s.SetProperty(c => c.Subject, new string('x', 32768)));
        var start = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.GenerateAsync(ReportKind.Contacts, ReportFormat.Excel, new(start, start.AddHours(1))));
    }

    [Fact]
    public async Task PdfEmbedsTurkishFontAndControllerDisablesCachingPrivateExports()
    {
        using var f = new ReportFixture(); var report = await f.Service.GenerateAsync(ReportKind.Contacts, ReportFormat.Pdf, new());
        Assert.StartsWith("%PDF", System.Text.Encoding.ASCII.GetString(report.Content.AsSpan(0, 8)));
        using var pdf = PdfReader.Open(new MemoryStream(report.Content), PdfDocumentOpenMode.Import); Assert.Equal(1, pdf.PageCount);
        Assert.Contains("BusinessLayer.Assets.Fonts.NotoSans-Regular.ttf", typeof(ReportService).Assembly.GetManifestResourceNames());
        var reports = new ReportsController(f.Service) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        var response = await reports.Contacts(); Assert.IsType<FileContentResult>(response); Assert.Equal("no-store", reports.Response.Headers.CacheControl);
        Assert.Equal(AccessPolicies.Moderator, typeof(ReportsController).GetMethod(nameof(ReportsController.Contacts))!.GetCustomAttributes<AuthorizeAttribute>().Single().Policy);
        foreach (var action in new[] { nameof(ReportsController.Blogs), nameof(ReportsController.Users), nameof(ReportsController.Interactions) })
            Assert.Equal(AccessPolicies.Admin, typeof(ReportsController).GetMethod(action)!.GetCustomAttributes<AuthorizeAttribute>().Single().Policy);
    }

    [Fact]
    public async Task ReportPreviewFixtureExportsAllFourKindsForVisualQa()
    {
        var before = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("tr-TR"); using var f = new ReportFixture();
            f.Db.AddRange(new BlogFavorite { AppUserId = 2, BlogId = 1 }, new BlogReaction { AppUserId = 2, BlogId = 1, ReactionType = ReactionType.Clap }); await f.Db.SaveChangesAsync();
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "WebUI.sln"))) directory = directory.Parent;
            Assert.NotNull(directory); var output = Path.Combine(directory!.FullName, ".artifacts", "stage3-report-preview"); Directory.CreateDirectory(output);
            foreach (var kind in Enum.GetValues<ReportKind>())
            {
                var excel = await f.Service.GenerateAsync(kind, ReportFormat.Excel, new());
                var pdf = await f.Service.GenerateAsync(kind, ReportFormat.Pdf, new());
                await File.WriteAllBytesAsync(Path.Combine(output, $"{kind.ToString().ToLowerInvariant()}.xlsx"), excel.Content);
                await File.WriteAllBytesAsync(Path.Combine(output, $"{kind.ToString().ToLowerInvariant()}.pdf"), pdf.Content);
            }
        }
        finally { CultureInfo.CurrentUICulture = before; }
    }
}
