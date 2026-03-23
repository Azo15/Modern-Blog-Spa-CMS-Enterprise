using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using System.Threading.Tasks;

namespace WebUI.Controllers
{
    [Authorize]
    public class AdminSliderController : BaseController
    {
        private readonly ISliderService _sliderService;
        private readonly IFileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public AdminSliderController(ISliderService sliderService, IFileHelper fileHelper, IWebHostEnvironment env)
        {
            _sliderService = sliderService;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "sliders");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var values = await _sliderService.GetListAsync(
                orderBy: (x => x.OrderByDescending(a => a.CreatedDate))
            );
            return View(values);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Slider model, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (ImageFile != null)
                model.Image = await _fileHelper.UploadAsync(ImageFile, _uploadPath);

            await _sliderService.AddAsync(model);
            SetToastMessage("Slider başarıyla oluşturuldu", "success");

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            var data = await _sliderService.GetByIDAsync(id.Value);

            if (data == null) return NotFound();

            return View(data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Slider model, int? id, IFormFile? NewImageFile)
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

                var success = await _sliderService.UpdateAsync(model);
                if (!success)
                {
                    SetToastMessage("Güncelleme işlemi başarısız", "error");
                    return View(model);
                }

                SetToastMessage("Slider başarıyla güncellendi", "success");
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

            var data = await _sliderService.GetByIDAsync(id.Value);

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

            var data = await _sliderService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound("Veri bulunamadı");
            }

            if (!string.IsNullOrEmpty(data.Image))
            {
                _fileHelper.Delete(data.Image);
            }

            var result = await _sliderService.DeleteAsync(id.Value);

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
