using System.Security.Claims;
using BusinessLayer.Security;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
namespace WebUI.Controllers;
[AllowAnonymous, Route("account/google")]
public sealed class ExternalAccountController(UserManager<AppUser> users, SignInManager<AppUser> signIn, IAuthenticationSchemeProvider schemes) : Controller
{
    private IActionResult Failure(string message)
    { TempData["ErrorMessage"] = message; return Redirect("/dashboard/giris"); }
    [HttpPost("start"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(string? returnUrl = null)
    {
        if (await schemes.GetSchemeAsync("Google") is null) return Failure("Google ile giriş henüz yapılandırılmadı. E-posta ve parolanızla devam edebilirsiniz.");
        var callback = Url.Action(nameof(Callback), new { returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/Profile" })!;
        return Challenge(signIn.ConfigureExternalAuthenticationProperties("Google", callback), "Google");
    }
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(string? returnUrl = null, string? remoteError = null)
    {
        if (remoteError is not null) return Failure("Google oturumu tamamlanamadı. Lütfen yeniden deneyin.");
        var info = await signIn.GetExternalLoginInfoAsync();
        if (info is null || info.LoginProvider != "Google") return Failure("Google oturumu doğrulanamadı.");
        var linked = await users.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (linked is not null)
        {
            if (!linked.IsActive || await users.IsLockedOutAsync(linked)) return Failure("Hesabınız kullanıma kapalı.");
            var result = await signIn.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, true, false);
            if (!result.Succeeded) return Failure("Bu hesap için ek doğrulama gerekiyor. E-posta ile giriş yapın.");
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Profile");
        }
        var email = VerifiedEmail(info);
        if (email is null) return Failure("Google doğrulanmış bir e-posta adresi sağlamadı.");
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null)
        {
            if (!existing.IsActive || await users.IsLockedOutAsync(existing)) return Failure("Hesabınız kullanıma kapalı.");
            // Keep Identity's pre-sign-in and two-factor checks for newly linked accounts too.
            var added = await users.AddLoginAsync(existing, info);
            if (!added.Succeeded)
            {
                var owner = await users.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (owner?.Id != existing.Id) return Failure("Google bağlantısı kurulamadı. Lütfen yeniden deneyin.");
            }
            if (!existing.EmailConfirmed)
            {
                existing.EmailConfirmed = true;
                if (!(await users.UpdateAsync(existing)).Succeeded) return Failure("Hesap doğrulaması tamamlanamadı. Lütfen yeniden deneyin.");
            }
            var result = await signIn.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, true, false);
            if (!result.Succeeded) return Failure("Bu hesap için ek doğrulama gerekiyor. E-posta ile giriş yapın.");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Profile");
        }
        ViewBag.GoogleEmail = email;
        return View("Register", new ExternalRegistrationModel { FirstName=info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "", LastName=info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "", ReturnUrl=Url.IsLocalUrl(returnUrl) ? returnUrl : "/Profile" });
    }
    private static string? VerifiedEmail(ExternalLoginInfo? info)
    {
        var principal = info?.Principal;
        var email = principal?.FindFirstValue(ClaimTypes.Email)?.Trim();
        return string.Equals(principal?.FindFirstValue("urn:xspa:email_verified"), "true", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(email) && new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)
            ? email : null;
    }
    [HttpPost("register"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(ExternalRegistrationModel model)
    {
        var info=await signIn.GetExternalLoginInfoAsync(); var email=info is null ? null : VerifiedEmail(info);
        if(info is null || info.LoginProvider!="Google" || string.IsNullOrWhiteSpace(email)) return Failure("Google oturumunuzun süresi doldu. Yeniden başlayın.");
        ViewBag.GoogleEmail=email;
        if(!ModelState.IsValid) return View(model);
        if(await users.FindByEmailAsync(email) is not null) return Failure("Bu adres zaten kayıtlı. Mevcut hesabınıza giriş yapın.");
        var user=new AppUser {UserName=email,Email=email,EmailConfirmed=true,FirstName=model.FirstName.Trim(),LastName=model.LastName.Trim(),PhoneNumber=model.Phone.Trim(),PrivacyAcknowledgedAtUtc=DateTime.UtcNow,PrivacyNoticeVersion="2026-10-18"};
        var created=await users.CreateAsync(user);
        if(!created.Succeeded){ModelState.AddModelError("","Hesap oluşturulamadı. Bilgilerinizi kontrol edin.");return View(model);}
        var login=await users.AddLoginAsync(user,info);
        var role=login.Succeeded ? await users.AddToRoleAsync(user,AppRoles.Customer) : IdentityResult.Failed();
        if(!login.Succeeded || !role.Succeeded){await users.DeleteAsync(user);return Failure("Hesap kaydı tamamlanamadı.");}
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        await signIn.SignInAsync(user,false);
        return LocalRedirect(Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl! : "/Profile");
    }
}
