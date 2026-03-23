using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;

namespace WebUI.Controllers
{
    [Authorize]
    public class AdminAboutCardController : BaseController
    {
        private readonly IAboutCardService _aboutCardService;
        private readonly IFileHelper _fileHelper;
        private readonly string _uploadPath;
        private readonly IWebHostEnvironment _env;

        public AdminAboutCardController(IAboutCardService aboutCardService, IFileHelper fileHelper, IWebHostEnvironment env)
        {
            _aboutCardService = aboutCardService;
            _fileHelper = fileHelper;
            _env = env;
            _uploadPath = Path.Combine(_env.WebRootPath, "uploads", "aboutsCard");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var values = await _aboutCardService.GetListAsync(orderBy: q => q.OrderByDescending(s => s.UpdatedDate));
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
        public async Task<IActionResult> Create(AboutCard model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _aboutCardService.AddAsync(model);

            SetToastMessage("Hakkımızda kartı başarıyla oluşturuldu", "success");
            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id <= 0 || id == null)
            {
                return NotFound();
            }

            var about = await _aboutCardService.GetByIDAsync(id.Value);

            if (about == null)
            {
                return NotFound();
            }
            return View(about);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AboutCard model, int id)
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
                var result = await _aboutCardService.UpdateAsync(model);

                if (!result)
                {
                    SetToastMessage("Güncelleme işlemi başarısız", "error");
                    return View(model);
                }

                SetToastMessage("Hakkımızda kartı başarıyla güncellendi", "success");
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

            var data = await _aboutCardService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound();
            }

            var vm = new GenericDeleteViewModel
            {
                Id = data.Id,
                Title = data.Title,
                SubHeading = data.Content,
                EntityName = "Hakkımda Kartı"
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

            var data = await _aboutCardService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound("Veri bulunamadı");
            }

            var result = await _aboutCardService.DeleteAsync(id.Value);

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
