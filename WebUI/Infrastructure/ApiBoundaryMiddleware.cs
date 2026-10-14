namespace WebUI.Infrastructure;

public sealed class ApiBoundaryMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (ApiProblemResponses.IsApi(context))
        {
            const long maximumBodyBytes = 1024 * 1024;
            if (context.Request.ContentLength > maximumBodyBytes)
            { await ApiProblemResponses.WriteAsync(context, StatusCodes.Status413PayloadTooLarge); return; }
            var limit = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
            if (limit is { IsReadOnly: false }) limit.MaxRequestBodySize = maximumBodyBytes;
        }
        await next(context);
        if (ApiProblemResponses.IsApi(context) && context.Response.StatusCode >= 400 &&
            !context.Response.HasStarted && string.IsNullOrEmpty(context.Response.ContentType) &&
            context.Response.ContentLength.GetValueOrDefault() == 0)
            await ApiProblemResponses.WriteAsync(context, context.Response.StatusCode);
    }
}
