using BusinessLayer.Abstract;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;

namespace WebUI.Controllers
{
    [Authorize(Policy = "RequireContentManager")]
    public class AdminCatagoryController : BaseController
    {
        private readonly ICatagoryService _catagoryService;

        public AdminCatagoryController(ICatagoryService catagoryService)
        {
            _catagoryService = catagoryService;
        }

        public async Task<IActionResult> Index()
        {
            var values = await _catagoryService.GetListAsync(
                orderBy: q => q.OrderByDescending(c => c.Title)
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
        public async Task<IActionResult> Create(Catagory model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await _catagoryService.AddAsync(model);
            SetToastMessage("Blog Kategorisi başarıyla oluşturuldu", "success");
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0)
            {
                return NotFound();
            }
            var values = await _catagoryService.GetByIDAsync(id.Value);

            if (values == null)
            {
                return NotFound();
            }

            return View(values);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Catagory model, int id)
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
                var success = await _catagoryService.UpdateAsync(model);
                if (!success)
                {
                    SetToastMessage("Güncelleme işlemi başarısız", "error");
                    return View(model);
                }

                SetToastMessage("Blog Kategorisi başarıyla güncellendi", "success");
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

            var data = await _catagoryService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound();
            }

            var vm = new GenericDeleteViewModel
            {
                Id = data.Id,
                Title = data.Title,
                EntityName = "Blok Kategorisi"
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

            var data = await _catagoryService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound("Veri bulunamadı");
            }

            var result = await _catagoryService.DeleteAsync(id.Value);

            if (!result)
            {
                SetToastMessage("Silme işleminde bir sorun oldu!", "error");
                return View(data);
            }

            SetToastMessage("Blok Kategorisi başarıyla silindi", "success");
            return RedirectToAction(nameof(Index));
        }
    }
}
