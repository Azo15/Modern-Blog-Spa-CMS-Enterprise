using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;

namespace WebUI.Controllers
{
    [Authorize]
    public class AdminAboutController : BaseController
    {
        private readonly IAboutService _aboutService;
        private readonly IFileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public AdminAboutController(IAboutService aboutService, IFileHelper fileHelper, IWebHostEnvironment env)
        {
            _aboutService = aboutService;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "abouts");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var values = await _aboutService.GetListAsync(orderBy: q => q.OrderByDescending(s => s.UpdatedDate));
            ViewBag.Count = values.Count;
            return View(values);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(About model, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var anyAboutExists = await _aboutService.AnyAsync();

            if (anyAboutExists)
            {
                SetToastMessage("Hakkımızda sayfası zaten mevcut. Yalnızca bir tane kayıt oluşturulabilir.", "warning");
                return RedirectToAction(nameof(Index));
            }

            if (ImageFile != null)
            {
                model.Image = await _fileHelper.UploadAsync(ImageFile, _uploadPath);
            }

            await _aboutService.AddAsync(model);

            SetToastMessage("Hakkımızda kayıdı başarıyla oluşturuldu", "success");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id <= 0 || id == null)
            {
                return NotFound();
            }

            var about = await _aboutService.GetByIDAsync(id.Value);

            if (about == null)
            {
                return NotFound();
            }
            return View(about);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(About model, int id, IFormFile? NewImageFile)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (NewImageFile != null)
                {
                    string? oldRelativePath = model.Image;
                    model.Image = await _fileHelper.UpdateAsync(NewImageFile, _uploadPath, oldRelativePath);
                }

                var result = await _aboutService.UpdateAsync(model);

                if (!result)
                {
                    SetToastMessage("Güncelleme işlemi başarısız", "error");
                    return View(model);
                }

                SetToastMessage("Hakkımızda kayıdı başarıyla güncellendi", "success");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetToastMessage("Güncelleme işlemi başarısız" + ex.Message, "error");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }

            var data = await _aboutService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound();
            }

            var vm = new GenericDeleteViewModel
            {
                Id = data.Id,
                Title = data.Title,
                SubHeading = data.Content,
                ImagePath = data.Image,
                EntityName = "Hakkımızda"
            };

            return View("Views/Shared/Delete.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound("ID bulunamadı");
            }

            var data = await _aboutService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound("Veri bulunamadı");
            }

            if (!string.IsNullOrEmpty(data.Image))
            {
                _fileHelper.Delete(data.Image);
            }

            var result = await _aboutService.DeleteAsync(id.Value);

            if (!result)
            {
                SetToastMessage("Silme işleminde bir sorun oldu!", "error");
                return View(data);
            }

            SetToastMessage("Admin başarıyla silindi", "success");
            return RedirectToAction(nameof(Index));
        }

    }
}
