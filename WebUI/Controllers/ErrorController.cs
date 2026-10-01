using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [AllowAnonymous]
    [Route("hata")]
    public class ErrorController : Controller
    {
        [Route("403")]
        public IActionResult AccessDenied()
        {
            return View();
        }
        [Route("404")]
        public IActionResult PageNotFound()
        {
            return View();
        }
    }
}
