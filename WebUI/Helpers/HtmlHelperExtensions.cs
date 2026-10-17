using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebUI.Helpers
{
    public static class HtmlHelperExtensions
    {
        public static string IsActive(this IHtmlHelper htmlHelper, string controller, string? action = null)
        {
            return htmlHelper.IsActive(controller, action, "active");
        }

        public static string IsActive(this IHtmlHelper htmlHelper, string controller, string? action, string result)
        {
            var routeData = htmlHelper.ViewContext.RouteData;

            var routeController = routeData.Values["controller"]?.ToString();
            var routeAction = routeData.Values["action"]?.ToString();

            bool isActive = routeController?.Equals(controller, StringComparison.OrdinalIgnoreCase) == true
                && (action == null || routeAction?.Equals(action, StringComparison.OrdinalIgnoreCase) == true);

            return isActive ? result : "";
        }
    }
}
