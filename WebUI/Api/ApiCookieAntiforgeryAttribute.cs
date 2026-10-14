using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebUI.Api;

/// <summary>Cookie based API commands require CSRF validation; validated bearer requests do not.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ApiCookieAntiforgeryAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Authentication chooses the bearer scheme before this filter runs. An arbitrary
        // Authorization header therefore cannot bypass CSRF for an authenticated cookie.
        if (context.HttpContext.User.Identity?.AuthenticationType == ApiAuthentication.BearerIdentityType) return;
        try
        {
            await context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "CSRF validation failed.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1"
            }) { StatusCode = StatusCodes.Status400BadRequest, ContentTypes = { "application/problem+json" } };
        }
    }
}
