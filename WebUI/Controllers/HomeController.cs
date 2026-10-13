using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebUI.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpPost, Microsoft.AspNetCore.Authorization.AllowAnonymous, ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string? returnUrl = null)
        {
            if (!LanguageSettings.Supported.Contains(culture, StringComparer.Ordinal)) return BadRequest();
            Response.Cookies.Append(Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.DefaultCookieName,
                Microsoft.AspNetCore.Localization.CookieRequestCultureProvider.MakeCookieValue(new Microsoft.AspNetCore.Localization.RequestCulture(culture)), new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true,
                    HttpOnly = true, Secure = Request.IsHttps, SameSite = SameSiteMode.Lax
                });
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
