using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Models;
using BusinessLayer;
using Microsoft.Extensions.Localization;

namespace WebUI.Controllers
{
    [Route("dashboard")]
    public class DashboardController : BaseController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IStringLocalizer<SharedResource> _text;

        public DashboardController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IStringLocalizer<SharedResource> text)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _text = text;
        }

        // Giriş (GET)
        [AllowAnonymous]
        [HttpGet("giris")]
        public IActionResult SignIn() => View();

        // Giriş (POST)
        [AllowAnonymous]
        [HttpPost("giris")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn(SignInViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrEmpty(model.Password))
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user != null)
            {
                if (!user.IsActive || await _userManager.IsLockedOutAsync(user))
                {
                    ModelState.AddModelError("", _text["AccountDisabled"]);
                    return View(model);
                }
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
                    user,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false
                );

                if (result.Succeeded)
                {
                    user.FailedLoginCount = 0;
                    user.LastFailedLogin = null;
                    await _userManager.UpdateAsync(user);
                    if (Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl!);
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
                    ModelState.AddModelError("", _text["InvalidCredentials"]);
                }
                return View(model);
            }

            ModelState.AddModelError("", _text["InvalidCredentials"]);
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
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrEmpty(model.Password))
                return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (existingUser != null)
            {
                ModelState.AddModelError("", _text["EmailAlreadyRegistered"]);
                return View(model);
            }

            var user = new AppUser
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = model.Email.Trim(),
                PhoneNumber = model.Phone.Trim(),
                PrivacyAcknowledgedAtUtc = DateTime.UtcNow,
                PrivacyNoticeVersion = "2026-10-18",
                UserName = model.Email.Trim()
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var roleResult = await _userManager.AddToRoleAsync(user, BusinessLayer.Security.AppRoles.Customer);
                if (!roleResult.Succeeded)
                {
                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError("", "Hesap kaydı tamamlanamadı. Lütfen yeniden deneyin.");
                    return View(model);
                }
                TempData["AccountSuccess"] = "XSPA’ya hoş geldiniz. Müşteri hesabınız hazır; giriş yapabilirsiniz.";
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
                return RedirectToAction("Index", "AdminDashboard");
            if (await _userManager.IsInRoleAsync(user, BusinessLayer.Security.AppRoles.Staff))
                return RedirectToAction("Index", "AdminDashboard");
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

