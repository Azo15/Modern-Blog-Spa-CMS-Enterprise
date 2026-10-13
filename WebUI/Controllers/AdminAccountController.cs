using BusinessLayer;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Security;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace WebUI.Controllers;

// This controller deliberately exposes no role assignment operations.
[Authorize(Policy = AccessPolicies.AccountModerator)]
public sealed class AdminAccountController(ISender sender, IStringLocalizer<SharedResource> text) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, string? search = null, CancellationToken ct = default)
    {
        ViewBag.Search = search;
        return View(await sender.Send(new GetModerationAccountsQuery(Math.Max(1, page), 20, search), ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLockout(int id, bool isLocked, int days = 30, CancellationToken ct = default)
    {
        try
        {
            if (!await sender.Send(new SetAccountLockoutCommand(id, isLocked, days), ct)) return NotFound();
            TempData["AccountSuccess"] = text["AccountUpdated"].Value;
        }
        catch (ValidationException ex) { TempData["AccountError"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
