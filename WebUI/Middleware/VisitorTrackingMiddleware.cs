using System.Security.Cryptography;
using System.Text;
using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using WebUI.Infrastructure;

namespace WebUI.Middleware;
public sealed class VisitorTrackingMiddleware(RequestDelegate next, ILogger<VisitorTrackingMiddleware> logger)
{
    private static readonly string[] PublicPaths = ["/ana-sayfa", "/hakkimizda", "/hizmetler", "/yapilan-isler", "/gorsel-galerisi", "/iletisim", "/randevu", "/kvkk", "/gizlilik-politikasi", "/kullanim-kosullari"];
    public static bool ShouldTrack(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method) || request.WantsJson()) return false;
        var path = request.Path.Value ?? "/";
        if (path.Length > 256 || path.Contains('.') || path.EndsWith("/yorumlar", StringComparison.OrdinalIgnoreCase)) return false;
        if (path != "/" && !PublicPaths.Any(prefix => request.Path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))) return false;
        var agent = request.Headers.UserAgent.ToString();
        return !string.IsNullOrWhiteSpace(agent) && !new[] { "bot", "spider", "crawler", "headless", "preview", "monitor", "curl", "wget", "python", "lighthouse", "slurp" }.Any(part => agent.Contains(part, StringComparison.OrdinalIgnoreCase));
    }
    public static string HashVisitor(string address, DateOnly day, string key) => Convert.ToHexString(HMACSHA256.HashData(
        Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes($"xspa:visitor:v1|{day:yyyy-MM-dd}|{address}")));

    public async Task InvokeAsync(HttpContext context, IVisitorAnalytics analytics, IConfiguration config)
    {
        var track = ShouldTrack(context.Request); await next(context);
        if (!track || context.Response.StatusCode != 200 || context.Response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) != true || context.Connection.RemoteIpAddress is null) return;
        var key = config["Analytics:HashKey"] ?? config["Jwt:SigningKey"];
        if (string.IsNullOrEmpty(key) || Encoding.UTF8.GetByteCount(key) < 32) return;
        try
        {
            var utc = DateTime.UtcNow; var day = VisitorAnalyticsCalendar.Day(utc);
            var hash = HashVisitor(context.Connection.RemoteIpAddress.MapToIPv6().ToString(), day, key);
            var agent = context.Request.Headers.UserAgent.ToString();
            var mobile = agent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) || agent.Contains("Android", StringComparison.OrdinalIgnoreCase) || agent.Contains("iPad", StringComparison.OrdinalIgnoreCase);
            await analytics.RecordAsync(day, hash, (context.Request.Path.Value ?? "/").ToLowerInvariant(), mobile, utc, context.RequestAborted);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        { logger.LogWarning("Visitor analytics unavailable ({ErrorType}); page delivery was unaffected.", error.GetType().Name); }
    }
}
