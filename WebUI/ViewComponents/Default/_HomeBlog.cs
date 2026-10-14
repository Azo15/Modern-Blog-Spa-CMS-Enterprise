using BusinessLayer.CQRS.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace WebUI.ViewComponents.Default;
public sealed class _HomeBlog(ISender sender) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()=>View((await sender.Send(new GetPublicBlogsQuery(1,3),HttpContext.RequestAborted)).Items);
}
