using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SpaMerkeziSablon.Controllers
{
    [AllowAnonymous]
    public class AuthorityController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
