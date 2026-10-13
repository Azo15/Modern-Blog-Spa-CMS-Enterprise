using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Models;

namespace WebUI.Controllers
{
    [Route("dashboard")]
    public class DashboardController : BaseController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public DashboardController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // Giriş (GET)
        [AllowAnonymous]
        [HttpGet("giris")]
        public IActionResult SignIn() => View();

        // Giriş (POST)
        [AllowAnonymous]
        [HttpPost("giris")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(SignInViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var now = DateTime.UtcNow;

                if (user.FailedLoginCount > 0 && user.LastFailedLogin.HasValue)
                {
                    var previousDelay = GetDelayForFailedCount(user.FailedLoginCount);
                    var nextTryTimeForPreviousLock = user.LastFailedLogin.Value.Add(previousDelay);

                    if (now < nextTryTimeForPreviousLock)
                    {
                        var remaining = (int)(nextTryTimeForPreviousLock - now).TotalSeconds;
                        TempData["RemainingSeconds"] = remaining;
                        return View(model);
                    }
                }

                // Giriş denemesi
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false
                );

                if (result.Succeeded)
                {
                    user.FailedLoginCount = 0;
                    user.LastFailedLogin = null;
                    await _userManager.UpdateAsync(user);
                    return await LandingPage(user);
                }

                // Başarısız giriş
                user.FailedLoginCount++;
                user.LastFailedLogin = now;
                await _userManager.UpdateAsync(user);

                var delayForNextTry = GetDelayForFailedCount(user.FailedLoginCount);
                if (delayForNextTry.TotalSeconds > 0)
                {
                    TempData["RemainingSeconds"] = (int)delayForNextTry.TotalSeconds;
                }
                else
                {
                    ModelState.AddModelError("", "Geçersiz e-posta veya şifre.");
                }
                return View(model);
            }

            ModelState.AddModelError("", "Geçersiz e-posta veya şifre.");
            return View(model);
        }
        // Kayıt (GET)

        [AllowAnonymous]
        [HttpGet("kayit")]
        public IActionResult SignUp() => View();


        [AllowAnonymous]
        [HttpPost("kayit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignUp(SignUpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Bu e-posta adresi zaten kayıtlı.");
                return View(model);
            }

            var user = new AppUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                UserName = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, BusinessLayer.Security.AppRoles.RegisteredUser);
                if (!roleResult.Succeeded) throw new InvalidOperationException("Üye rolü atanamadı.");
                return RedirectToAction(nameof(SignIn));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        private async Task<IActionResult> LandingPage(AppUser user)
        {
            if (await _userManager.IsInRoleAsync(user, BusinessLayer.Security.AppRoles.Admin))
                return RedirectToAction("UserList", "AdminUserList");
            if (await _userManager.IsInRoleAsync(user, BusinessLayer.Security.AppRoles.ContentManager)
                || await _userManager.IsInRoleAsync(user, BusinessLayer.Security.AppRoles.Author))
                return RedirectToAction("Index", "AdminBlog");
            if (await _userManager.IsInRoleAsync(user, BusinessLayer.Security.AppRoles.Moderator))
                return RedirectToAction("Index", "AdminMessage");
            return RedirectToAction("Index", "Default");
        }

        private TimeSpan GetDelayForFailedCount(int failedCount)
        {
            return failedCount switch
            {
                1 => TimeSpan.FromSeconds(10),
                2 => TimeSpan.FromSeconds(15),
                3 => TimeSpan.FromSeconds(20),
                4 => TimeSpan.FromSeconds(30),
                5 => TimeSpan.FromSeconds(40),
                _ => TimeSpan.FromMinutes(1)
            };
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Default");
        }
    }
}

