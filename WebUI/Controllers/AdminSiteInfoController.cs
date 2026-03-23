using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers
{
    [Authorize]
    public class AdminSiteInfoController : BaseController
    {
        private readonly ISiteTableService _siteTableService;
        private readonly IFileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public AdminSiteInfoController(ISiteTableService siteTableService, IFileHelper fileHelper, IWebHostEnvironment env)
        {
            _siteTableService = siteTableService;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "siteInfo");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var contact = (await _siteTableService.GetListAsync()).FirstOrDefault();

            if (contact == null)
            {
                ViewData["Title"] = "Site Tablosu Oluştur";
                ViewData["Submit"] = "Kaydet";
                return View(new SiteTable());
            }

            ViewData["Title"] = "Site Tablosu Düzenle";
            ViewData["Submit"] = "Güncelle";
            return View(contact);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SiteTable model, IFormFile? NewImageFile, IFormFile? NewSocialMediaFile)
        {
            if (!ModelState.IsValid)
            {
                ViewData["Title"] = model.Id == 0 ? "Site Tablosu Oluştur" : "Site Tablosu Düzenle";
                ViewData["Submit"] = model.Id == 0 ? "Kaydet" : "Güncelle";
                SetToastMessage("Tüm gerekli alanları doldurunuz.", "warning");
                return View(model);
            }

            try
            {
                if (model.Id == 0)
                {
                    var existing = (await _siteTableService.GetListAsync()).FirstOrDefault();

                    if (existing != null)
                    {
                        SetToastMessage("Zaten bir Sit Tablo kaydı mevcut. Yeni kayıt ekleyemezsiniz.", "warning");
                        return RedirectToAction(nameof(Index));
                    }

                    if (NewImageFile != null)
                    {
                        model.SiteLogo = await _fileHelper.UploadAsync(NewImageFile, _uploadPath);
                    }

                    if (NewSocialMediaFile != null)
                    {
                        model.SocialImage = await _fileHelper.UploadAsync(NewSocialMediaFile, _uploadPath);
                    }

                    if (await _siteTableService.AddAsync(model))
                    {
                        SetToastMessage("Site tablosu başarıyla eklendi.", "success");
                    }
                    else
                    {
                        SetToastMessage("Ekleme işlemi başarısız oldu.", "error");
                    }
                }
                else
                {
                    if (NewImageFile != null)
                    {
                        string? oldRelativePath = model.SiteLogo;
                        model.SiteLogo = await _fileHelper.UpdateAsync(NewImageFile, _uploadPath, oldRelativePath);
                    }

                    if (NewSocialMediaFile != null)
                    {
                        string? oldRelativePath = model.SocialImage;
                        model.SocialImage = await _fileHelper.UpdateAsync(NewSocialMediaFile, _uploadPath, oldRelativePath);
                    }

                    if (await _siteTableService.UpdateAsync(model))
                    {
                        SetToastMessage("Site tablosu başarıyla güncellendi.", "success");
                    }
                    else
                    {
                        SetToastMessage("Güncelleme işlemi başarısız oldu.", "error");
                    }
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                SetToastMessage($"Bir hata oluştu: {ex.Message}", "error");
                return View(model);
            }
        }
    }
}
