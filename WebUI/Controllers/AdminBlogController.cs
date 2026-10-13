using AutoMapper;
using BusinessLayer.Abstract;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using EntityLayer.Concrete;
using EntityLayer.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebUI.Models;
using static WebUI.Helpers.GlobalHelper;

namespace WebUI.Controllers;

[Authorize(Policy = AccessPolicies.BlogAuthor)]
public class AdminBlogController(IBlogService blogs, ICatagoryService categories, IFileHelper files,
    IWebHostEnvironment environment, IMapper mapper, IAuthorizationService authorization,
    IBlogWorkflowService workflow) : BaseController
{
    private string UploadPath => Path.Combine(environment.WebRootPath, "uploads", "blogs");
    private async Task<bool> CanManage(Blog blog) =>
        (await authorization.AuthorizeAsync(User, blog, AccessPolicies.BlogResource)).Succeeded;

    public async Task<IActionResult> Index(int? categoryId)
    {
        var query = blogs.GetQueryable();
        if (!(await authorization.AuthorizeAsync(User, AccessPolicies.ContentManager)).Succeeded)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) return Forbid();
            query = query.Where(b => b.AppUserId == id);
        }
        var data = await query.Where(b => !categoryId.HasValue || b.CatagoryId == categoryId)
            .Include(b => b.AppUser).Include(b => b.Catagory).OrderByDescending(b => b.CreatedDate).ToListAsync();
        ViewBag.blogCount = data.Count;
        ViewBag.SelectedCategory = categoryId;
        ViewBag.Categories = await CategoryList();
        return View(data);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await CategoryList();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BlogCreateDto model, IFormFile? ImageFile)
    {
        if (!ModelState.IsValid) { ViewBag.Categories = await CategoryList(); return View(model); }
        var image = ImageFile is null ? null : await files.UploadAsync(ImageFile, UploadPath);
        await workflow.CreateAsync(new Blog { Title = model.Title, SubTitle = model.SubTitle,
            Content = model.Content, Slug = ToSlug(model.Title), CatagoryId = model.CatagoryId, Image = image });
        SetToastMessage("Blog taslak olarak kaydedildi", "success");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null or <= 0) return NotFound();
        var blog = await blogs.GetByIDAsync(id.Value);
        if (blog is null) return NotFound();
        if (!await CanManage(blog)) return Forbid();
        ViewBag.Categories = await CategoryList();
        return View(mapper.Map<BlogUpdateDto>(blog));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BlogUpdateDto model, int id, IFormFile? NewImageFile)
    {
        if (id != model.Id) return NotFound();
        var blog = await blogs.GetByIDAsync(id);
        if (blog is null) return NotFound();
        if (!await CanManage(blog)) return Forbid();
        if (!ModelState.IsValid) { model.Image = blog.Image; ViewBag.Categories = await CategoryList(); return View(model); }
        var image = NewImageFile is null ? null : await files.UploadAsync(NewImageFile, UploadPath);
        if (!await workflow.UpdateAsync(id, model.Title, model.SubTitle, model.Content, model.CatagoryId, image)) return NotFound();
        SetToastMessage("Blog güncellendi", "success");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null or <= 0) return NotFound();
        var blog = await blogs.GetByIDAsync(id.Value);
        if (blog is null) return NotFound();
        if (!await CanManage(blog)) return Forbid();
        return View("/Views/Shared/Delete.cshtml", new GenericDeleteViewModel {
            Id = blog.Id, Title = blog.Title, SubHeading = blog.SubTitle, ImagePath = blog.Image, EntityName = "Blog" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int? id)
    {
        if (id is null or <= 0) return NotFound();
        var blog = await blogs.GetByIDAsync(id.Value);
        if (blog is null) return NotFound();
        if (!await CanManage(blog)) return Forbid();
        if (!await workflow.DeleteAsync(id.Value)) return NotFound();
        SetToastMessage("Blog çöp kutusuna taşındı. Görsel korundu.", "success");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForReview(int id)
    {
        var blog = await blogs.GetByIDAsync(id);
        if (blog is null) return NotFound();
        if (!await CanManage(blog)) return Forbid();
        if (!await workflow.ChangeStatusAsync(id, PublishStatus.PendingReview)) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = AccessPolicies.ContentManager)]
    public async Task<IActionResult> Review(int id, PublishStatus status)
    {
        if (status is not (PublishStatus.Published or PublishStatus.Rejected or PublishStatus.Draft)) return BadRequest();
        if (!await workflow.ChangeStatusAsync(id, status)) return NotFound();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet, Authorize(Policy = AccessPolicies.ContentManager)]
    public async Task<IActionResult> Trash() => View(await workflow.GetTrashAsync());

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = AccessPolicies.ContentManager)]
    public async Task<IActionResult> Restore(int id)
    {
        if (!await workflow.RestoreAsync(id)) return NotFound();
        SetToastMessage("Blog görseliyle birlikte taslak olarak geri yüklendi", "success");
        return RedirectToAction(nameof(Trash));
    }

    private async Task<List<SelectListItem>> CategoryList() => (await categories.GetListAsync())
        .Select(c => new SelectListItem(c.Title, c.Id.ToString())).ToList();
}
