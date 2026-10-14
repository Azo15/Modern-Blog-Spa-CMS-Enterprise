using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Infrastructure;

public static class ApiProblemResponses
{
    public static bool IsApi(HttpContext context) => context.Request.Path.StartsWithSegments("/api/v1", StringComparison.OrdinalIgnoreCase);

    public static Task WriteAsync(HttpContext context, int status, string? title = null,
        IDictionary<string, string[]>? errors = null)
    {
        ProblemDetails problem = errors is null ? new ProblemDetails() : new ValidationProblemDetails(errors);
        problem.Status = status;
        problem.Title = title ?? status switch
        {
            400 => "Request validation failed.", 401 => "Authentication required.", 403 => "Access denied.",
            404 => "Resource not found.", 405 => "Method not allowed.", 409 => "Concurrent operation conflict.",
            413 => "Request body is too large.", 429 => "Too many requests.", _ => "An unexpected server error occurred."
        };
        problem.Type = "about:blank";
        problem.Instance = context.Request.Path;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        context.Response.StatusCode = status;
        context.Response.Headers.CacheControl = "no-store";
        return problem is ValidationProblemDetails validation
            ? context.Response.WriteAsJsonAsync(validation, options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted)
            : context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}
