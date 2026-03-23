using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Authorize]
    public class AdminGalaryController : BaseController
    {
        private readonly IGalaryService _galaryService;
        private readonly IFileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public AdminGalaryController(IGalaryService galaryService, IFileHelper fileHelper, IWebHostEnvironment env)
        {
            _galaryService = galaryService;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "galeries");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _galaryService.GetListAsync(
                orderBy: x => x.OrderByDescending(a => a.CreatedDate)
            );

            ViewBag.galaryCount = data.Count;

            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(List<IFormFile> Images)
        {
            var descriptions = Request.Form["Descriptions"].ToList();

            if (Images != null && Images.Any())
            {
                for (int i = 0; i < Images.Count; i++)
                {
                    var file = Images[i];

                    var extension = Path.GetExtension(file.FileName).ToLower();
                    if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".webp")
                        continue;

                    // Resmi kaydet
                    var relativePath = await _fileHelper.UploadAsync(file, _uploadPath);
                    if (string.IsNullOrEmpty(relativePath))
                        continue;

                    // Açıklama al
                    var description = descriptions.ElementAtOrDefault(i) ?? "";

                    var image = new Galary
                    {
                        Image = relativePath,
                        CreatedDate = DateTime.Now,
                        Title = description
                    };

                    SetToastMessage("Galeri başarıyla güncellendi", "success");
                    await _galaryService.AddAsync(image);
                }
            }
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                SetToastMessage("Silinecek resim seçilmedi!", "danger");
                return RedirectToAction(nameof(Index));
            }

            int successCount = 0;
            int failCount = 0;

            foreach (var id in ids)
            {
                var data = await _galaryService.GetByIDAsync(id);

                if (data != null)
                {
                    // Dosyayı sil
                    if (!string.IsNullOrEmpty(data.Image))
                    {
                        _fileHelper.Delete(data.Image); // FileHelper class’ı relative path alıyor
                    }

                    await _galaryService.DeleteAsync(data.Id);
                    successCount++;
                }
                else
                {
                    failCount++;
                }
            }

            if (successCount > 0)
            {
                SetToastMessage($"{successCount} resim başarıyla silindi.", "success");
            }

            if (failCount > 0)
            {
                SetToastMessage($"{failCount} resim silinemedi veya bulunamadı.", "danger");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
