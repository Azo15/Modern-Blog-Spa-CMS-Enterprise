using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;

namespace WebUI.ViewComponents;

public sealed class BlogInteractionViewComponent(ISender sender, IActiveMemberService members) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int blogId, string? slug)
        => View(new BlogInteractionViewModel(blogId, slug, await sender.Send(new GetBlogInteractionQuery(blogId), HttpContext.RequestAborted),
            await members.IsActiveAsync(HttpContext.User, HttpContext.RequestAborted)));
}
