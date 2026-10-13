using System.Security.Claims;
using BusinessLayer;
using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Security;
using EntityLayer.Concrete;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WebUI.Models;

namespace WebUI.Controllers;

[Authorize(Policy = AccessPolicies.ActiveMember)]
public sealed class ProfileController(UserManager<AppUser> users, SignInManager<AppUser> signIn,
    ISender sender, IImageOptimizationService images, IStringLocalizer<SharedResource> text) : Controller
{
    private Task<AppUser?> Current() => users.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "");

    private async Task<ProfileViewModel> Build(AppUser user, ProfileEditModel? edit, CancellationToken ct, int page = 1)
        => new(edit ?? new ProfileEditModel { FirstName = user.FirstName ?? "", LastName = user.LastName ?? "" },
            user.Email, user.Image, await sender.Send(new GetUserFavoritesQuery(page), ct));

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
        var user = await Current();
        if (user is null) return Forbid();
        return View(await Build(user, null, ct, Math.Max(1, page)));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileEditModel model, CancellationToken ct)
    {
        var user = await Current();
        if (user is null || await users.IsLockedOutAsync(user) || !user.IsActive) return Forbid();
        if (!ModelState.IsValid) return View("Index", await Build(user, model, ct));
        string? avatar = null;
        try { avatar = await images.StoreAsync(model.Avatar, "avatars", ct); }
        catch (ValidationException ex) { ModelState.AddModelError(nameof(model.Avatar), ex.Message); return View("Index", await Build(user, model, ct)); }
        user.FirstName = model.FirstName.Trim(); user.LastName = model.LastName.Trim();
        if (avatar is not null) user.Image = avatar;
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View("Index", await Build(user, model, ct));
        }
        await signIn.RefreshSignInAsync(user);
        TempData["ProfileSuccess"] = text["ProfileUpdated"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ProfilePasswordModel model, CancellationToken ct)
    {
        var user = await Current();
        if (user is null || await users.IsLockedOutAsync(user) || !user.IsActive) return Forbid();
        if (!ModelState.IsValid) return View("Index", await Build(user, null, ct));
        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View("Index", await Build(user, null, ct));
        }
        await signIn.RefreshSignInAsync(user);
        TempData["ProfileSuccess"] = text["PasswordChanged"].Value;
        return RedirectToAction(nameof(Index));
    }
}
