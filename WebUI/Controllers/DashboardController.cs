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

        public DashboardController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, IStringLocalizer<SharedResource> text, IPasswordResetMailService resetMail, IConfiguration configuration, ILogger<DashboardController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _text = text;
            _resetMail = resetMail; _configuration = configuration; _logger = logger;
        }

        private readonly IPasswordResetMailService _resetMail;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DashboardController> _logger;
        private const string ResetNotice = "Eğer bu e-posta adresi sistemimizde kayıtlıysa, şifre sıfırlama bağlantısı gönderilmiştir.";

        [AllowAnonymous, HttpGet("sifremi-unuttum"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

        [AllowAnonymous, HttpPost("sifremi-unuttum"), ValidateAntiForgeryToken, RequestSizeLimit(16384), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user is { IsActive: true })
            {
                try
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
                    // Never build a reset link from an untrusted Host header.
                    var origin = new Uri(_configuration["Authentication:PublicOrigin"] ?? "https://xspawellness.runasp.net");
                    if (origin.Scheme != "https" || !string.IsNullOrEmpty(origin.UserInfo)) throw new InvalidOperationException("Invalid public origin.");
                    var path = Url.Action(nameof(ResetPassword), "Dashboard", new { userId = user.Id, token = encoded })!;
                    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
                    deadline.CancelAfter(TimeSpan.FromSeconds(90));
                    var resetUrl = new Uri(origin, path).AbsoluteUri;
                    var googleOnly = !await _userManager.HasPasswordAsync(user)
                        && (await _userManager.GetLoginsAsync(user)).Any(login => login.LoginProvider == "Google");
                    if (googleOnly) await _resetMail.SendGoogleAccountAsync(user.Email!, resetUrl, deadline.Token);
                    else await _resetMail.SendAsync(user.Email!, resetUrl, deadline.Token);
                }
                catch (Exception error) when (!HttpContext.RequestAborted.IsCancellationRequested)
                { _logger.LogWarning("Password reset delivery did not complete ({ErrorType}).", error.GetType().Name); }
            }
            TempData["ResetNotice"] = ResetNotice;
            return RedirectToAction(nameof(ForgotPassword));
        }

        [AllowAnonymous, HttpGet("sifre-sifirla"), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult ResetPassword(string? userId, string? token)
        {
            Response.Headers["Referrer-Policy"] = "no-referrer";
            if (!int.TryParse(userId, out var id) || id <= 0 || string.IsNullOrWhiteSpace(token) || token.Length > 4096)
            { TempData["ResetError"] = "Bağlantı geçersiz. Yeni bir sıfırlama bağlantısı isteyin."; return RedirectToAction(nameof(ForgotPassword)); }
            return View(new ResetPasswordViewModel { UserId = userId, Token = token });
        }

        [AllowAnonymous, HttpPost("sifre-sifirla"), ValidateAntiForgeryToken, RequestSizeLimit(16384), ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            Response.Headers["Referrer-Policy"] = "no-referrer";
            if (!ModelState.IsValid) return View(model);
            if (!int.TryParse(model.UserId, out var userId) || userId <= 0)
            { ModelState.AddModelError("", "Bağlantı geçersiz veya süresi dolmuş. Yeni bağlantı isteyin."); return View(model); }
            var user = await _userManager.FindByIdAsync(userId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            string token;
            try { token = System.Text.Encoding.UTF8.GetString(Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(model.Token)); }
            catch (FormatException) { ModelState.AddModelError("", "Bağlantı geçersiz veya süresi dolmuş. Yeni bağlantı isteyin."); return View(model); }
            var result = user is { IsActive: true } ? await _userManager.ResetPasswordAsync(user, token, model.Password) : IdentityResult.Failed();
            if (!result.Succeeded)
            { ModelState.AddModelError("", "Bağlantı geçersiz veya süresi dolmuş; yeni bağlantı isteyin ve parola kurallarını kontrol edin."); return View(model); }
            user!.FailedLoginCount = 0; user.LastFailedLogin = null; await _userManager.UpdateAsync(user);
            TempData["AccountSuccess"] = "Parolanız yenilendi. Yeni parolanızla giriş yapabilirsiniz.";
            return RedirectToAction(nameof(SignIn));
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

