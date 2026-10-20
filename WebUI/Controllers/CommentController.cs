using BusinessLayer.CQRS.Commands;
using BusinessLayer.Security;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Infrastructure;
namespace WebUI.Controllers;
[Authorize(Policy = AccessPolicies.Member)]
public sealed class CommentController(ISender sender, Microsoft.Extensions.Localization.IStringLocalizer<BusinessLayer.SharedResource> text) : Controller
{
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int blogId, string content, int? parentCommentId, string slug, CancellationToken ct)
    {
        try {
            var id = await sender.Send(new CreateCommentCommand(blogId, content ?? "", parentCommentId), ct);
            if (Request.WantsJson()) return Json(new { success = true, comment = new { id, blogId, parentCommentId, content = content?.Trim(), isApproved = false }, message = text["CommentSubmitted"].Value });
            TempData["CommentSuccess"] = text["CommentSubmitted"].Value;
        }
        catch (ValidationException ex) { if (Request.WantsJson()) return BadRequest(new { success = false, message = ex.Message }); TempData["CommentError"] = ex.Message; }
        return RedirectToAction("Post", "Blog", new { slug });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, string content, string slug, CancellationToken ct)
    {
        try {
            if (!await sender.Send(new UpdateCommentCommand(id, content ?? ""), ct)) return NotFound();
            if (Request.WantsJson()) return Json(new { success = true, comment = new { id, content = content?.Trim(), isApproved = false }, message = text["CommentUpdated"].Value });
            TempData["CommentSuccess"] = text["CommentUpdated"].Value;
        }
        catch (ValidationException ex) { if (Request.WantsJson()) return BadRequest(new { success = false, message = ex.Message }); TempData["CommentError"] = ex.Message; }
        return RedirectToAction("Post", "Blog", new { slug });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string slug, CancellationToken ct)
    {
        if (!await sender.Send(new DeleteCommentCommand(id), ct)) return NotFound();
        if (Request.WantsJson()) return Json(new { success = true, id,
            message = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "en" ? "Comment deleted." : "Yorum silindi." });
        return RedirectToAction("Post", "Blog", new { slug });
    }
}
