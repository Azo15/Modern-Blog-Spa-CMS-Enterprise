using AutoMapper;
using BusinessLayer.Abstract;
using BusinessLayer.Dtos;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebUI.Models;
using static WebUI.Helpers.GlobalHelper;

namespace WebUI.Controllers
{
    [Authorize]
    public class AdminBlogController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly string _uploadPath;
        private readonly IBlogService _blogService;
        private readonly ICatagoryService _catagoryService;
        private readonly IFileHelper _fileHelper;
        private readonly IWebHostEnvironment _env;

        public AdminBlogController(IBlogService blogService, ICatagoryService catagoryService, IFileHelper fileHelper, IWebHostEnvironment env, IMapper mapper)
        {
            _blogService = blogService;
            _catagoryService = catagoryService;
            _fileHelper = fileHelper;
            _env = env;
            _mapper = mapper;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "blogs");
        }

        public async Task<IActionResult> Index(int? categoryId)
        {
            var data = await _blogService.GetQueryable()
                .Where(b => !categoryId.HasValue || b.CatagoryId == categoryId.Value)
                .OrderByDescending(b => b.CreatedDate)
                .Include(b => b.AppUser)
                .Include(b => b.Catagory)
                .ToListAsync();

            ViewBag.blogCount = data.Count;
            ViewBag.SelectedCategory = categoryId;
            ViewBag.Categories = await GetCategorySelectList();

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await GetCategorySelectList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BlogCreateDto model, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await GetCategorySelectList();
                return View(model);
            }

            model.Slug = ToSlug(model.Title);
            var blog = _mapper.Map<Blog>(model);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            blog.AppUserId = userId;

            if (ImageFile != null)
                blog.Image = await _fileHelper.UploadAsync(ImageFile, _uploadPath);

            await _blogService.AddAsync(blog);
            SetToastMessage("Blog başarıyla oluşturuldu", "success");

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            var data = await _blogService.GetByIDAsync(id.Value);

            if (data == null) return NotFound();

            var model = _mapper.Map<BlogUpdateDto>(data);

            ViewBag.Categories = await GetCategorySelectList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BlogUpdateDto model, int id, IFormFile? NewImageFile)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await GetCategorySelectList();
                return View(model);
            }

            try
            {
                var existingBlog = await _blogService.GetByIDAsync(id);
                if (existingBlog == null) return NotFound();

                model.Slug = ToSlug(model.Title);
                _mapper.Map(model, existingBlog);

                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                existingBlog.AppUserId = userId;

                if (NewImageFile != null)
                {
                    existingBlog.Image = await _fileHelper.UpdateAsync(NewImageFile, _uploadPath, existingBlog.Image);
                }

                var success = await _blogService.UpdateAsync(existingBlog);

                if (!success)
                {
                    SetToastMessage("Güncelleme işlemi başarısız", "error");
                    return View(model);
                }

                SetToastMessage("Blog başarıyla güncellendi", "success");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetToastMessage("Güncelleme işlemi başarısız: " + ex.Message, "error");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var data = await _blogService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound();
            }

            var vm = new GenericDeleteViewModel
            {
                Id = data.Id,
                Title = data.Title,
                SubHeading = data.SubTitle,
                ImagePath = data.Image,
                EntityName = "Blok"
            };

            return View("/Views/Shared/Delete.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id <= 0 || id == null)
            {
                return NotFound("ID bulunamadı");
            }

            var data = await _blogService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound("Veri bulunamadı");
            }

            if (!string.IsNullOrEmpty(data.Image))
            {
                _fileHelper.Delete(data.Image);
            }

            var result = await _blogService.DeleteAsync(id.Value);

            if (!result)
            {
                SetToastMessage("Silme işleminde bir sorun oldu!", "error");
                return View(data);
            }

            SetToastMessage("Blok başarıyla silindi", "success");
            return RedirectToAction(nameof(Index));
        }

        // Ortak kategori select list metodu
        private async Task<List<SelectListItem>> GetCategorySelectList()
        {
            var categories = await _catagoryService.GetListAsync();
            return categories.Select(x => new SelectListItem
            {
                Text = x.Title,
                Value = x.Id.ToString()
            }).ToList();
        }
    }
}
