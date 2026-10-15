using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace WebUI.Infrastructure;

public static class ApiRateLimitRegistration
{
    public static IServiceCollection AddPlatformRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            // Resolve after the host's complete configuration has been applied.
            var permits = configuration.GetValue("ApiRateLimit:PermitLimit", 60);
            var windowSeconds = configuration.GetValue("ApiRateLimit:WindowSeconds", 60);
            if (permits is < 1 or > 10000 || windowSeconds is < 1 or > 3600)
                throw new InvalidOperationException("API rate limit configuration is outside the supported range.");
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (HttpMethods.IsPost(context.Request.Method) && context.Request.Path.Equals("/randevu", StringComparison.OrdinalIgnoreCase))
                    return RateLimitPartition.GetFixedWindowLimiter("appointment:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown"),
                        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0, AutoReplenishment = true });
                if (!ApiProblemResponses.IsApi(context)) return RateLimitPartition.GetNoLimiter("web-ui");
                var id = context.User.Identity?.IsAuthenticated == true ? context.User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
                var key = id is null ? "ip:" + (context.Connection.RemoteIpAddress?.ToString() ?? "unknown") : "user:" + id;
                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                { PermitLimit = permits, Window = TimeSpan.FromSeconds(windowSeconds), QueueLimit = 0, AutoReplenishment = true });
            });
            options.OnRejected = async (rejected, cancellationToken) =>
            {
                if (rejected.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    rejected.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                await ApiProblemResponses.WriteAsync(rejected.HttpContext, StatusCodes.Status429TooManyRequests);
            };
        });
        return services;
    }
}
