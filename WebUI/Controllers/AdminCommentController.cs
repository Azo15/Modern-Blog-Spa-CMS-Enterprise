using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Security;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace WebUI.Controllers;
[Authorize(Policy = AccessPolicies.Moderator)]
[Authorize(Policy = AccessPolicies.CommentModerator)]
public sealed class AdminCommentController(ISender sender) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default) => View(await sender.Send(new GetPendingCommentsQuery(page), ct));
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        try { if (!await sender.Send(new ApproveCommentCommand(id), ct)) return NotFound(); }
        catch (ValidationException ex) { TempData["CommentError"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (!await sender.Send(new DeleteCommentCommand(id), ct)) return NotFound(); return RedirectToAction(nameof(Index));
    }
}
