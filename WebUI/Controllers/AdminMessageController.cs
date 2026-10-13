using BusinessLayer.Abstract;
using EntityLayer.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;

namespace WebUI.Controllers
{
    [Authorize(Policy = "RequireModerator")]
    public class AdminMessageController : BaseController
    {
        private readonly IContactFormService _contactFormService;

        public AdminMessageController(IContactFormService contactFormService)
        {
            _contactFormService = contactFormService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await _contactFormService.GetListAsync(
                filter: x => x.Status == ReservationStatus.SoforBeklemede,
                orderBy: x => x.OrderByDescending(a => a.CreatedDate)
            );

            ViewBag.Count = data.Count();

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
            {
                return NotFound("Geçersiz ID.");
            }

            var contactForm = await _contactFormService.GetByIDAsync(id);

            if (contactForm == null)
            {
                return NotFound("İlgili kayıt bulunamadı.");
            }

            return View(contactForm);
        }

        //[HttpGet]
        //public async Task<IActionResult> Delete(int? id)
        //{
        //    if (id == null || id <= 0)
        //    {
        //        return NotFound();
        //    }

        //    var data = await _contactFormService.GetByIDAsync(id.Value);

        //    if (data == null)
        //    {
        //        return NotFound();
        //    }

        //    var vm = new GenericDeleteViewModel
        //    {
        //        Id = data.Id,
        //        Title = data.Title,
        //        SubHeading = data.Descriptions,
        //        Icon = "Gelen Mail"
        //    };

        //    return View("Views/Shared/Delete.cshtml", vm);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(int? id)
        //{
        //    if (id == null || id <= 0)
        //    {
        //        return NotFound("ID bulunamadı");
        //    }

        //    var data = await _messageService.GetByIDAsync(id.Value);

        //    if (data == null)
        //    {
        //        return NotFound("Veri bulunamadı");
        //    }

        //    var result = await _messageService.DeleteAsync(id.Value);

        //    if (!result)
        //    {
        //        SetToastMessage("Silme işleminde bir sorun oldu!", "error");
        //        return View(data);
        //    }

        //    SetToastMessage("Mail Kayıdı başarıyla silindi", "success");
        //    return RedirectToAction(nameof(Index));
        //}
    }
}
