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
    [Authorize(Policy = BusinessLayer.Security.AccessPolicies.Admin)]
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

            var allowed = BusinessLayer.Security.AppRoles.All;
            var roles = await _roleManager.Roles.Where(x => allowed.Contains(x.Name!)).ToListAsync();
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

            var selected = model.Where(x => x.RoleExist).ToList();
            if (selected.Count != 1 || !BusinessLayer.Security.AppRoles.All.Contains(selected[0].RoleName)) return BadRequest("Bir temel rol seçin.");
            var target = await _roleManager.FindByIdAsync(selected[0].RoleId.ToString());
            if (target?.Name != selected[0].RoleName) return BadRequest();
            var previous = await _userManager.GetRolesAsync(user);
            if (previous.Contains(BusinessLayer.Security.AppRoles.Admin) && target.Name != BusinessLayer.Security.AppRoles.Admin && (await _userManager.GetUsersInRoleAsync(BusinessLayer.Security.AppRoles.Admin)).Count <= 1)
                return BadRequest("Son sistem yöneticisinin rolü kaldırılamaz.");
            if (!previous.Contains(target.Name))
            {
                var assigned = await _userManager.AddToRoleAsync(user, target.Name);
                if (!assigned.Succeeded) return BadRequest("Rol atanamadı.");
            }
            var removed = await _userManager.RemoveFromRolesAsync(user, previous.Where(x => x != target.Name));
            if (!removed.Succeeded) return BadRequest("Önceki roller kaldırılamadı.");
            await _userManager.UpdateSecurityStampAsync(user);

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
