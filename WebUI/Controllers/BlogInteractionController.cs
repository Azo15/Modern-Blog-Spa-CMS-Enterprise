using BusinessLayer;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.Security;
using EntityLayer.Enum;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using WebUI.Infrastructure;
using BusinessLayer.Dtos;

namespace WebUI.Controllers;

[Authorize(Policy = AccessPolicies.ActiveMember)]
public sealed class BlogInteractionController(ISender sender, IStringLocalizer<SharedResource> text) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Favorite(int blogId, string? slug, CancellationToken ct)
    {
        try { var result = await sender.Send(new ToggleBlogFavoriteCommand(blogId), ct); if (Request.WantsJson()) return State(result); TempData["InteractionSuccess"] = text["InteractionUpdated"].Value; }
        catch (ValidationException ex) { if (Request.WantsJson()) return BadRequest(new { success = false, message = ex.Message }); TempData["InteractionError"] = ex.Message; }
        return RedirectToAction("Post", "Blog", new { slug });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reaction(int blogId, ReactionType? reactionType, string? slug, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try { var result = await sender.Send(new SetBlogReactionCommand(blogId, reactionType), ct); if (Request.WantsJson()) return State(result); TempData["InteractionSuccess"] = text["InteractionUpdated"].Value; }
        catch (ValidationException ex) { if (Request.WantsJson()) return BadRequest(new { success = false, message = ex.Message }); TempData["InteractionError"] = ex.Message; }
        return RedirectToAction("Post", "Blog", new { slug });
    }
    private JsonResult State(BlogInteractionDto state) => Json(new { success = true, state.IsFavorite, state.FavoriteCount,
        reaction = state.Reaction is null ? (int?)null : (int)state.Reaction, state.LikeCount, state.HeartCount, state.ClapCount,
        message = text["InteractionUpdated"].Value });
}
