using System.Net;
using System.Text.Json;

namespace WebUI.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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
                await context.Response.WriteAsJsonAsync(new { message = "Doğrulama başarısız.", errors = exception.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
            }
            catch (UnauthorizedAccessException)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beklenmeyen bir sunucu hatası meydana geldi! Path: {Path}, Method: {Method}",
                    context.Request.Path, context.Request.Method);

                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
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
                    message = "Sunucu tarafında beklenmeyen bir hata oluştu.",
                    detail = exception.Message
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
