
using BusinessLayer.Abstract;
using BusinessLayer.CQRS.Queries;
using DataAccesLayer.Concrete;
using EntityLayer.Concrete;
using MediatR;
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
        private readonly IBlogService _blogService;
        private readonly IMediator _mediator;

        public BlogController(IBlogService blogService, IMediator mediator)
        {
            _blogService = blogService;
            _mediator = mediator;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 20;
            
            // 📌 CQRS MediatR Query Kullanımı
            var allBlogs = await _mediator.Send(new GetAllBlogsQuery());

            int totalItems = allBlogs.Count;

            var data = allBlogs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

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

            // 📌 CQRS MediatR Query Kullanımı (Kategoriye Göre İlgili Bloglar)
            var categoryBlogs = await _mediator.Send(new GetAllBlogsQuery(blog.CatagoryId));
            var relatedBlogs = categoryBlogs
                .Where(x => x.Id != blog.Id)
                .Take(4)
                .ToList();

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
