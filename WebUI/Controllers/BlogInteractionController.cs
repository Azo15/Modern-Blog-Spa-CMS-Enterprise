using BusinessLayer;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.Security;
using EntityLayer.Enum;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace WebUI.Controllers;

[Authorize(Policy = AccessPolicies.ActiveMember)]
public sealed class BlogInteractionController(ISender sender, IStringLocalizer<SharedResource> text) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Favorite(int blogId, string? slug, CancellationToken ct)
    {
        try { await sender.Send(new ToggleBlogFavoriteCommand(blogId), ct); TempData["InteractionSuccess"] = text["InteractionUpdated"].Value; }
        catch (ValidationException ex) { TempData["InteractionError"] = ex.Message; }
        return RedirectToAction("Post", "Blog", new { slug });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reaction(int blogId, ReactionType? reactionType, string? slug, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try { await sender.Send(new SetBlogReactionCommand(blogId, reactionType), ct); TempData["InteractionSuccess"] = text["InteractionUpdated"].Value; }
        catch (ValidationException ex) { TempData["InteractionError"] = ex.Message; }
        return RedirectToAction("Post", "Blog", new { slug });
    }
}
