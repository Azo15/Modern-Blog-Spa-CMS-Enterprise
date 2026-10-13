using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using EntityLayer.Enum;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using WebUI.Models;
namespace WebUI.Controllers;

[Authorize(Policy = AccessPolicies.BlogAuthor)]
public sealed class AdminBlogController(ISender sender) : BaseController
{
    public async Task<IActionResult> Index(int? categoryId, int page = 1, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetBlogsQuery(page, 20, categoryId), ct);
        ViewBag.SelectedCategory = categoryId; ViewBag.blogCount = result.TotalCount;
        await Categories(ct); return View(result);
    }
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken ct) { await Categories(ct); return View(); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogCreateDto model, IFormFile? ImageFile, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try { await sender.Send(new CreateBlogCommand(model.Title, model.SubTitle, model.Content, model.CatagoryId, ImageFile), ct); return RedirectToAction(nameof(Index)); }
            catch (ValidationException ex) { AddErrors(ex); }
        }
        await Categories(ct); return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Edit(int? id, CancellationToken ct)
    {
        if (id is null or <= 0) return NotFound();
        var blog = await sender.Send(new GetBlogByIdQuery(id.Value), ct);
        if (blog is null) return NotFound();
        await Categories(ct);
        return View(new BlogUpdateDto { Id = blog.Id, Title = blog.Title ?? "", SubTitle = blog.SubTitle ?? "", Content = blog.Content ?? "", Slug = blog.Slug, Image = blog.Image, CatagoryId = blog.CatagoryId ?? 0, IsPublish = blog.PublishStatus == PublishStatus.Published });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BlogUpdateDto model, int id, IFormFile? NewImageFile, CancellationToken ct)
    {
        if (id != model.Id || id <= 0) return NotFound();
        var blog = await sender.Send(new GetBlogByIdQuery(id), ct);
        if (blog is null) return NotFound();
        if (ModelState.IsValid)
        {
            try { if (!await sender.Send(new UpdateBlogCommand(id, model.Title, model.SubTitle, model.Content, model.CatagoryId, NewImageFile), ct)) return NotFound(); return RedirectToAction(nameof(Index)); }
            catch (ValidationException ex) { AddErrors(ex); }
        }
        model.Image = blog.Image; await Categories(ct); return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Delete(int? id, CancellationToken ct)
    {
        if (id is null or <= 0) return NotFound();
        var blog = await sender.Send(new GetBlogByIdQuery(id.Value), ct);
        if (blog is null) return NotFound();
        return View("/Views/Shared/Delete.cshtml", new GenericDeleteViewModel { Id = blog.Id, Title = blog.Title, SubHeading = blog.SubTitle, ImagePath = blog.Image, EntityName = "Blog" });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id, CancellationToken ct)
    {
        if (id is null or <= 0) return NotFound();
        if (!await sender.Send(new DeleteBlogCommand(id.Value), ct)) return NotFound();
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForReview(int id, CancellationToken ct)
    {
        if (!await sender.Send(new ChangeBlogStatusCommand(id, PublishStatus.PendingReview), ct)) return NotFound();
        return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = AccessPolicies.ContentManager)]
    public async Task<IActionResult> Review(int id, PublishStatus status, CancellationToken ct)
    {
        if (status is not (PublishStatus.Published or PublishStatus.Rejected or PublishStatus.Draft)) return BadRequest();
        if (!await sender.Send(new ChangeBlogStatusCommand(id, status), ct)) return NotFound();
        return RedirectToAction(nameof(Index));
    }
    [HttpGet, Authorize(Policy = AccessPolicies.ContentManager)]
    public async Task<IActionResult> Trash(int page = 1, CancellationToken ct = default) => View(await sender.Send(new GetTrashedBlogsQuery(page), ct));
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = AccessPolicies.ContentManager)]
    public async Task<IActionResult> Restore(int id, CancellationToken ct)
    {
        if (!await sender.Send(new RestoreBlogCommand(id), ct)) return NotFound(); return RedirectToAction(nameof(Trash));
    }
    private async Task Categories(CancellationToken ct) => ViewBag.Categories = (await sender.Send(new GetCategoryOptionsQuery(), ct)).Select(c => new SelectListItem(c.Title, c.Id.ToString())).ToList();
    private void AddErrors(ValidationException ex)
    {
        if (!ex.Errors.Any()) ModelState.AddModelError("", ex.Message);
        foreach (var error in ex.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
    }
}
