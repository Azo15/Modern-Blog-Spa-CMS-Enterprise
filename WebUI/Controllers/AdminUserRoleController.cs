using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Models;
using MediatR;
using BusinessLayer.CQRS.Commands;
using FluentValidation;

namespace WebUI.Controllers
{
    [Authorize(Policy = "RequireAdmin")]
    public class AdminUserRoleController : BaseController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly ISender _sender;

        public AdminUserRoleController(UserManager<AppUser> userManager, RoleManager<IdentityRole<int>> roleManager, ISender sender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoleList = new List<UserWithRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLockedOut = await _userManager.IsLockedOutAsync(user);

                userRoleList.Add(new UserWithRolesViewModel
                {
                    UserId = user.Id,
                    FullName = $"{user.FirstName} {user.LastName}",
                    Email = user.Email ?? string.Empty,
                    Image = user.Image,
                    Roles = roles.ToList(),
                    IsLockedOut = isLockedOut
                });
            }

            return View(userRoleList);
        }

        [HttpGet]
        public async Task<IActionResult> AssignRole(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                SetToastMessage("Kullanıcı bulunamadı.", "error");
                return RedirectToAction(nameof(Index));
            }

            ViewBag.UserName = $"{user.FirstName} {user.LastName}";
            ViewBag.UserId = user.Id;

            var roles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var roleAssignList = new List<UserRoleAssignViewModel>();

            foreach (var role in roles)
            {
                roleAssignList.Add(new UserRoleAssignViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty,
                    RoleExist = userRoles.Contains(role.Name ?? string.Empty)
                });
            }

            return View(roleAssignList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRole(int userId, List<UserRoleAssignViewModel> model)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                SetToastMessage("Kullanıcı bulunamadı.", "error");
                return RedirectToAction(nameof(Index));
            }

            foreach (var item in model)
            {
                if (item.RoleExist)
                {
                    if (!await _userManager.IsInRoleAsync(user, item.RoleName))
                    {
                        await _userManager.AddToRoleAsync(user, item.RoleName);
                    }
                }
                else
                {
                    if (await _userManager.IsInRoleAsync(user, item.RoleName))
                    {
                        await _userManager.RemoveFromRoleAsync(user, item.RoleName);
                    }
                }
            }

            SetToastMessage("Kullanıcı roller başarıyla güncellendi.", "success");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == id)
            {
                SetToastMessage("Güvenlik sebebiyle kendi hesabınızı kilitleyemezsiniz.", "warning");
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                SetToastMessage("Kullanıcı bulunamadı.", "error");
                return RedirectToAction(nameof(Index));
            }

            var isLocked = await _userManager.IsLockedOutAsync(user);
            try
            {
                if (!await _sender.Send(new SetAccountLockoutCommand(id, !isLocked, 3650))) return NotFound();
                SetToastMessage(isLocked ? "Kullanıcı kilidi başarıyla kaldırıldı." : "Kullanıcı hesabı kilitlendi.", isLocked ? "success" : "warning");
            }
            catch (ValidationException ex)
            {
                SetToastMessage(ex.Message, "warning");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
