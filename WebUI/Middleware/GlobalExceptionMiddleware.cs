using System.Net;
using System.Text.Json;

namespace WebUI.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly Microsoft.Extensions.Localization.IStringLocalizer<BusinessLayer.SharedResource>? _text;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger,
            Microsoft.Extensions.Localization.IStringLocalizer<BusinessLayer.SharedResource>? text = null)
        {
            _next = next;
            _logger = logger;
            _text = text;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (FluentValidation.ValidationException exception)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new { message = _text?["ValidationFailed"].Value ?? "Doğrulama başarısız.", errors = exception.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
            }
            catch (UnauthorizedAccessException)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            }
            catch (BusinessLayer.Exceptions.InteractionConflictException)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                await context.Response.WriteAsJsonAsync(new { message = _text?["InteractionConflict"].Value ?? "The action conflicted with another request. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beklenmeyen bir sunucu hatası meydana geldi! Path: {Path}, Method: {Method}",
                    context.Request.Path, context.Request.Method);

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var isAjaxOrApi = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                              context.Request.Headers["Accept"].ToString().Contains("application/json");

            if (isAjaxOrApi)
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = context.Response.StatusCode,
                    message = _text?["ServerError"].Value ?? "Sunucu tarafında beklenmeyen bir hata oluştu."
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            else
            {
                context.Response.Redirect("/Home/Error");
            }
        }
    }
}
