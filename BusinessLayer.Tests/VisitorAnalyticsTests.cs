using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using BusinessLayer.Jobs;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebUI.Middleware;

namespace BusinessLayer.Tests;
public sealed class VisitorAnalyticsTests
{
    [Theory]
    [InlineData("/admin", "Mozilla/5.0", false)]
    [InlineData("/AdminMessage/Details/1", "Mozilla/5.0", false)]
    [InlineData("/dashboard/giris", "Mozilla/5.0", false)]
    [InlineData("/api/blogs", "Mozilla/5.0", false)]
    [InlineData("/css/site.css", "Mozilla/5.0", false)]
    [InlineData("/yapilan-isler/article/yorumlar", "Mozilla/5.0", false)]
    [InlineData("/ana-sayfa", "Googlebot", false)]
    [InlineData("/ana-sayfa", "", false)]
    [InlineData("/randevularim", "Mozilla/5.0", false)]
    [InlineData("/yapilan-isler/article", "Mozilla/5.0 Android Mobile", true)]
    [InlineData("/hizmetler/1", "Mozilla/5.0", true)]
    public void FilterExcludesPrivateStaticAndAutomatedTraffic(string path, string agent, bool expected)
    {
        var context = new DefaultHttpContext(); context.Request.Method = "GET"; context.Request.Path = path; context.Request.Headers.UserAgent = agent;
        Assert.Equal(expected, VisitorTrackingMiddleware.ShouldTrack(context.Request));
        context.Request.Method = "POST"; Assert.False(VisitorTrackingMiddleware.ShouldTrack(context.Request));
    }
    [Fact]
    public void VisitorHashRotatesDailyAndCannotBeReproducedWithoutTheSecret()
    {
        var day = new DateOnly(2026, 10, 21); var key = new string('k', 32);
        var hash = VisitorTrackingMiddleware.HashVisitor("192.0.2.1", day, key);
        Assert.Equal(64, hash.Length); Assert.Equal(hash, VisitorTrackingMiddleware.HashVisitor("192.0.2.1", day, key));
        Assert.NotEqual(hash, VisitorTrackingMiddleware.HashVisitor("192.0.2.1", day.AddDays(1), key));
        Assert.NotEqual(hash, VisitorTrackingMiddleware.HashVisitor("192.0.2.1", day, new string('b', 32)));
    }
    [Fact]
    public async Task StorageFailureDoesNotBreakPageDeliveryAndNoQueryStringIsRecorded()
    {
        var context = new DefaultHttpContext(); context.Request.Method = "GET"; context.Request.Path = "/iletisim"; context.Request.QueryString = new QueryString("?email=private@example.test");
        context.Request.Headers.UserAgent = "Mozilla/5.0 Android Mobile"; context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.0.2.1");
        var analytics = new Mock<IVisitorAnalytics>();
        analytics.Setup(x => x.RecordAsync(It.IsAny<DateOnly>(), It.IsAny<string>(), "/iletisim", true, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException());
        var middleware = new VisitorTrackingMiddleware(c => { c.Response.ContentType = "text/html"; return Task.CompletedTask; }, NullLogger<VisitorTrackingMiddleware>.Instance);
        await middleware.InvokeAsync(context, analytics.Object, new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SigningKey"] = new string('k', 32) }).Build());
        Assert.Equal(200, context.Response.StatusCode); analytics.Verify(x => x.RecordAsync(It.IsAny<DateOnly>(), It.Is<string>(hash => hash.Length == 64), "/iletisim", true, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
    private sealed class Clock(DateTime now) : TimeProvider { public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero); }
    [Fact]
    public async Task AggregationIsIdempotentPreservesSummariesAfterRetentionAndSeparatesDevices()
    {
        using var f = new Stage2Fixture(); using var cache = new MemoryCache(new MemoryCacheOptions());
        var now = new DateTime(2026, 10, 21, 21, 6, 0, DateTimeKind.Utc); var today = VisitorAnalyticsCalendar.Day(now); var yesterday = today.AddDays(-1);
        var analytics = new VisitorAnalytics(f.Db, cache);
        await analytics.RecordAsync(yesterday, "visitor-a", "/ana-sayfa", true, now.AddDays(-1), default);
        await analytics.RecordAsync(yesterday, "visitor-a", "/ana-sayfa", true, now.AddDays(-1), default);
        await analytics.RecordAsync(yesterday, "visitor-b", "/hizmetler", false, now.AddDays(-1), default);
        var old = today.AddDays(-35); await analytics.RecordAsync(old, "old-visitor", "/hakkimizda", false, now.AddDays(-35), default);
        var job = new DailyVisitorAggregationJob(f.Db, analytics, NullLogger<DailyVisitorAggregationJob>.Instance, new Clock(now));
        await job.RunAsync(default); await job.RunAsync(default); f.Db.ChangeTracker.Clear();
        var stats = await f.Db.DailyVisitorStats.SingleAsync(x => x.Date == yesterday);
        Assert.Equal(3, stats.TotalPageViews); Assert.Equal(2, stats.UniqueVisitors); Assert.Equal(2, stats.MobileVisits); Assert.Equal(1, stats.DesktopVisits);
        Assert.Contains("ana-sayfa", stats.TopVisitedPagesJson); Assert.Equal(3, await f.Db.VisitorLogs.CountAsync());
        Assert.Equal(1, (await f.Db.DailyVisitorStats.SingleAsync(x => x.Date == old)).UniqueVisitors);
        Assert.Equal(0, (await analytics.GetDayAsync(today, default)).UniqueVisitors);
        Assert.Equal(new DateOnly(2026, 10, 22), today); // 00:06 in Türkiye
    }
}
