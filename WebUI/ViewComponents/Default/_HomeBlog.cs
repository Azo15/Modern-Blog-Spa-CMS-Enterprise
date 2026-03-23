using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace WebUI.ViewComponents.Default
{
    public class _HomeBlog : ViewComponent
    {
        private readonly IBlogService _blogService;

        public _HomeBlog(IBlogService blogService)
        {
            _blogService = blogService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var data = await _blogService.GetListAsync(
                filter: x => x.IsPublish == true,
                orderBy: q => q.OrderByDescending(x => x.CreatedDate),
                take: 8,
                includes: new Expression<Func<Blog, object>>[] { x => x.AppUser, x => x.Catagory }
            );

            return View(data);
        }
    }
}
