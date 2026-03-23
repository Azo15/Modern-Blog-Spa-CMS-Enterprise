using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;

namespace WebUI.Controllers
{
    [Authorize]
    public class AdminServiceController : BaseController
    {
        private readonly IServiceService _serviceService;
        private readonly IFileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public AdminServiceController(IServiceService serviceService, IFileHelper fileHelper, IWebHostEnvironment env)
        {
            _serviceService = serviceService;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "services");
        }

        public async Task<IActionResult> Index()
        {
            var data = await _serviceService.GetListAsync(
                orderBy:(q => q.OrderByDescending(b => b.CreatedDate))
            );

            ViewBag.Count = (await _serviceService.GetListAsync()).Count;

            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Service model, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (ImageFile != null)
                model.Image = await _fileHelper.UploadAsync(ImageFile, _uploadPath);

            await _serviceService.AddAsync(model);
            SetToastMessage("Blog başarıyla oluşturuldu", "success");

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            var data = await _serviceService.GetByIDAsync(id.Value);

            if (data == null) return NotFound();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Service model, int id, IFormFile? NewImageFile)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (NewImageFile != null)
                {
                    model.Image = await _fileHelper.UpdateAsync(NewImageFile, _uploadPath, model.Image);
                }

                var success = await _serviceService.UpdateAsync(model);
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

            var data = await _serviceService.GetByIDAsync(id.Value);

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

            var data = await _serviceService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound("Veri bulunamadı");
            }

            if (!string.IsNullOrEmpty(data.Image))
            {
                _fileHelper.Delete(data.Image);
            }

            var result = await _serviceService.DeleteAsync(id.Value);

            if (!result)
            {
                SetToastMessage("Silme işleminde bir sorun oldu!", "error");
                return View(data);
            }

            SetToastMessage("Blok başarıyla silindi", "success");
            return RedirectToAction(nameof(Index));
        }
    }
}
