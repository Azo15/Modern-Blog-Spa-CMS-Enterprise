
using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Linq.Expressions;
using WebUI.Helpers;
using WebUI.Models;

namespace WebUI.Controllers
{
    [AllowAnonymous]
    [Route("yapilan-isler")]
    public class BlogController : Controller
    {

        private readonly AppDbContext _context;
        private readonly IBlogService _blogService;

        public BlogController(AppDbContext context, IBlogService blogService)
        {
            _context = context;
            _blogService = blogService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 20;
            var query = _blogService.GetQueryable();

            int totalItems = await query.CountAsync();

            var data = await query
                .Include(x => x.AppUser)
                .Include(x => x.Catagory)
                .OrderByDescending(b => b.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalBlogs = totalItems;

            ViewBag.PaginationInfo = new PaginationInfo
            {
                CurrentPage = page,
                ItemsPerPage = pageSize,
                TotalItems = totalItems,
                Controller = "Blog",
                Action = "Index"
            };

            return View(data);
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> Post(string slug)
        {
            if (slug.IsNullOrEmpty())
            {
                return NotFound();
            }

            var blog = await _blogService.FirstOrDefaultAsync(
                predicate: b => b.Slug == slug,
                orderBy: null,
                includes: new Expression<Func<Blog, object>>[] { x => x.AppUser, x => x.Catagory }
            );

            if (blog == null)
            {
                return NotFound();
            }

            var relatedBlogs = await _blogService.GetListAsync(
                filter: x => x.CatagoryId == blog.CatagoryId && x.Id != blog.Id && x.IsPublish == true,
                orderBy: q => q.OrderByDescending(b => b.CreatedDate),
                take: 4,
                includes: new Expression<Func<Blog, object>>[] { x => x.AppUser, x => x.Catagory }
            );

            ViewBag.RelatedBlogs = relatedBlogs;

            ViewBag.ReadingTime = BlogUtils.CalculateReadingTime(blog.Content);

            return View(blog);
        }


        // YArdımcı metotlar okunma süresi hesalama

        public static class BlogUtils
        {
            public static int CalculateReadingTime(string content)
            {
                if (string.IsNullOrWhiteSpace(content))
                    return 0;

                var wordCount = content.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;

                // Ortalama 200 kelime/dk
                int wordsPerMinute = 200;

                // Yuvarlayarak dakika cinsinden döndür
                return (int)Math.Ceiling((double)wordCount / wordsPerMinute);
            }
        }
    }
}
