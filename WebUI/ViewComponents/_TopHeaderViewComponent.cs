using EntityLayer.Concrete;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;

namespace WebUI.ViewsComponents
{
    public class _TopHeaderViewComponent : ViewComponent
    {
        private readonly UserManager<AppUser> _userManager;

        public _TopHeaderViewComponent(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user == null)
            {
                return View(null);
            }

            var model = new TopHeaderViewModel
            {
                FullName = user.FirstName + " " + user.LastName,
                Email = user.Email,
                ImagePath = user.Image ?? "~/img/default-image.jpg"
            };

            return View(model);
        }
    }
}

