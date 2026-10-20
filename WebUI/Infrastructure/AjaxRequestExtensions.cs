namespace WebUI.Infrastructure;

public static class AjaxRequestExtensions
{
    public static bool WantsJson(this HttpRequest request) =>
        string.Equals(request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
        request.GetTypedHeaders().Accept?.Any(value =>
            string.Equals(value.MediaType.Value, "application/json", StringComparison.OrdinalIgnoreCase)) == true;
}
