using BusinessLayer.CQRS.Queries;
using BusinessLayer.Dtos;
using BusinessLayer.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
namespace WebUI.Controllers;

[Authorize(Policy = AccessPolicies.Visitor)]
[Route("yapilan-isler")]
public sealed class BlogController(ISender sender, IAuthorizationService authorization) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, int? categoryId = null, CancellationToken ct = default)
    {
        var result = await sender.Send(new GetPublicBlogsQuery(page, 20, categoryId), ct);
        ViewBag.TotalBlogs = result.TotalCount;
        ViewBag.CategoryId = categoryId;
        return View(result);
    }
    [HttpGet("{slug}")]
    public async Task<IActionResult> Post(string slug, int commentPage = 1, CancellationToken ct = default)
    {
        var blog = await sender.Send(new GetPublicBlogBySlugQuery(slug), ct);
        if (blog is null) return NotFound();
        var comments = await sender.Send(new GetPublicCommentsQuery(blog.Id, commentPage), ct);
        var own = (await authorization.AuthorizeAsync(User, AccessPolicies.Member)).Succeeded ? await sender.Send(new GetMyCommentsQuery(blog.Id), ct) : new List<OwnCommentDto>();
        ViewBag.RelatedBlogs = (await sender.Send(new GetPublicBlogsQuery(1, 4, blog.CatagoryId, blog.Id), ct)).Items;
        ViewBag.ReadingTime = BlogUtils.CalculateReadingTime(blog.Content);
        return View(new BlogPostViewModel(blog, comments, own));
    }
    public static class BlogUtils
    {
        public static int CalculateReadingTime(string? content) => string.IsNullOrWhiteSpace(content) ? 0 : (int)Math.Ceiling(content.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length / 200.0);
    }
}
