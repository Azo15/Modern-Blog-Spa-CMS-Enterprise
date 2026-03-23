using BusinessLayer.Abstract;
using DataAccesLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

namespace WebUI.Controllers
{

    [AllowAnonymous]
    public class ServiceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IServiceService _serviceService;

        public ServiceController(AppDbContext context, IServiceService serviceService)
        {
            _context = context;
            _serviceService = serviceService;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 20;
            var query = _serviceService.GetQueryable();

            int totalItems = await query.CountAsync();

            var data = await query
                .OrderByDescending(s => s.UpdatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalBlogs = totalItems;
            
            ViewBag.PaginationInfo = new WebUI.Models.PaginationInfo
            {
                CurrentPage = page,
                ItemsPerPage = pageSize,
                TotalItems = totalItems,
                UrlFormat = "/Service?page={0}"
            };
            return View(data);
        }

        [HttpGet]
        public IActionResult ServiceDetails(int? id)
        {
            if(id == null || id <= 0)
            {
                return NotFound();
            }

            var values = _serviceService.GetByIDAsync(id.Value);

            if (values == null)
            {
                return NotFound();
            }
            return View(values);
        }
    }
}
