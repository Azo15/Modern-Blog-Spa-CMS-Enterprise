using BusinessLayer.Abstract;
using BusinessLayer.Concrete;
using EntityLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
namespace WebUI.Controllers
{
    [Authorize(Policy = "RequireContentManager")]
    public class AdminContactController : BaseController
    {
        private readonly IContactService _contactService;

        public AdminContactController(IContactService contactService)
        {
            _contactService = contactService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _contactService.GetListAsync(
                orderBy: x => x.OrderByDescending(a => a.CreatedDate)
            );
            ViewBag.Count = data.Count;
            return View(data);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Contact model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var contactData = await _contactService.AnyAsync();

            if (contactData)
            {
                SetToastMessage("İletişim bilgileri zaten mevcut. Yalnızca bir tane kayıt oluşturulabilir.", "warning");
                return RedirectToAction(nameof(Index));
            }
            await _contactService.AddAsync(model);
            SetToastMessage("İletişim Bilgileri başarıyla oluşturuldu", "success");
            return RedirectToAction("Index", "AdminContact");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || id <= 0) return NotFound();

            var data = await _contactService.GetByIDAsync(id.Value);

            if (data == null) return NotFound();

            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Contact model, int id)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var success = await _contactService.UpdateAsync(model);
                if (!success)
                {
                    SetToastMessage("Güncelleme işlemi başarısız", "error");
                    return View(model);
                }

                SetToastMessage("İletişim Bilgileri başarıyla güncellendi", "success");
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

            var data = await _contactService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound();
            }

            var vm = new GenericDeleteViewModel
            {
                Id = data.Id,
                Title = data.Mail,
                SubHeading = data.Phone,
                EntityName = "İletişim Bilgisi"
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

            var data = await _contactService.GetByIDAsync(id.Value);

            if (data == null)
            {
                return NotFound("Veri bulunamadı");
            }

            var result = await _contactService.DeleteAsync(id.Value);

            if (!result)
            {
                SetToastMessage("Silme işleminde bir sorun oldu!", "error");
                return View(data);
            }

            SetToastMessage("İletişim Bilgisi başarıyla silindi", "success");
            return RedirectToAction(nameof(Index));
        }
    }
}