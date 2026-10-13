using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Models;

namespace WebUI.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class AdminUserListController : BaseController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IFileHelper _fileHelper;

        public AdminUserListController(UserManager<AppUser> userManager, IFileHelper fileHelper)
        {
            _userManager = userManager;
            _fileHelper = fileHelper;
        }

        [HttpGet]
        public async Task<IActionResult> UserList()
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                SetToastMessage("Kullanıcı bulunamadı", "error");
                return RedirectToAction("Index", "Dashboard");
            }

            var users = await _userManager.Users
                .Select(u => new UserListViewModel
                {
                    Id = u.Id.ToString(),
                    FullName = $"{u.FirstName} {u.LastName}",
                    Email = u.Email,
                    Image = u.Image
                })
                .ToListAsync();

            ViewBag.Count = users.Count();

            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                SetToastMessage("Kullanıcı bulunamadı", "error");
                return RedirectToAction("Index", "Dashboard");
            }

            if (id == currentUser.Id)
            {
                SetToastMessage("Güvenlik nedeniyle kendi hesabınızı silemezsiniz.", "warning");
                return RedirectToAction(nameof(UserList));
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                SetToastMessage("Kullanıcı bulunamadı", "error");
                return RedirectToAction(nameof(UserList));
            }

            IdentityResult result;
            try
            {
                result = await _userManager.DeleteAsync(user);
            }
            catch (DbUpdateException)
            {
                // Restrict preserves comment/audit history, including soft-deleted comments.
                SetToastMessage("İlişkili kayıtları bulunan kullanıcı silinemez. Hesabı kilitleyebilirsiniz.", "warning");
                return RedirectToAction(nameof(UserList));
            }

            if (!result.Succeeded)
            {
                SetToastMessage("Kullanıcı silinemedi", "error");
            }
            else
            {
                if (!string.IsNullOrEmpty(user.Image)) _fileHelper.Delete(user.Image);
                SetToastMessage("Kullanıcı başarıyla silindi", "success");
            }

            return RedirectToAction(nameof(UserList));
        }

    }
}
