using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Security;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;
using WebUI.Infrastructure;

namespace WebUI.Controllers;
[Authorize(Policy = AccessPolicies.ActiveMember)]
[Route("randevularim")]
public sealed class MyAppointmentsController(ISender sender) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
        => View(await sender.Send(new GetMyAppointmentsQuery(Math.Clamp(page, 1, 100000)), ct));

    [HttpGet("{id:int}/duzenle")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var a = await sender.Send(new GetMyAppointmentQuery(id), ct);
        if (a is null) return NotFound();
        if (!a.CanChange) { TempData["AppointmentNotice"] = "Bu randevu artık değiştirilemez."; return RedirectToAction(nameof(Index)); }
        var model = new MyAppointmentEditModel { Id = a.Id, ServiceId = a.ServiceId, AppointmentDate = a.AppointmentDate,
            TimeSlot = a.TimeSlot, GuestCount = a.GuestCount, Notes = a.Notes, ExpectedUpdatedDate = a.UpdatedDate };
        await Populate(model, ct); return View(model);
    }
    [HttpPost("{id:int}/duzenle"), ValidateAntiForgeryToken, RequestSizeLimit(16384)]
    public async Task<IActionResult> Edit(int id, MyAppointmentEditModel model, CancellationToken ct)
    {
        if (id != model.Id) return NotFound();
        if (await sender.Send(new GetMyAppointmentQuery(id), ct) is null) return NotFound();
        if (ModelState.IsValid)
        {
            try
            {
                if (!await sender.Send(new UpdateMyAppointmentCommand(id, model.ServiceId, model.AppointmentDate,
                    model.TimeSlot, model.GuestCount, model.Notes, model.ExpectedUpdatedDate), ct)) return NotFound();
                if (Request.WantsJson()) return Json(new { success = true,
                    updatedDate = (await sender.Send(new GetMyAppointmentQuery(id), ct))?.UpdatedDate,
                    message = "Randevunuz güncellendi. Yeni bilgiler işletmenin onayına gönderildi." });
                TempData["AppointmentNotice"] = "Randevunuz güncellendi. Yeni bilgiler işletmenin onayına gönderildi.";
                return RedirectToAction(nameof(Index));
            }
            catch (ValidationException e) { ModelState.AddModelError("", e.Message); }
        }
        if (Request.WantsJson()) return BadRequest(new { success = false, message = "Randevu güncellenemedi. Bilgileri kontrol edin.",
            errors = ModelState.Where(e => e.Value?.Errors.Count > 0).ToDictionary(e => e.Key, e => e.Value!.Errors.Select(error => error.ErrorMessage).ToArray()) });
        await Populate(model, ct); return View(model);
    }
    [HttpPost("{id:int}/iptal"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, DateTime? expectedUpdatedDate, CancellationToken ct)
    {
        try
        {
            if (!await sender.Send(new CancelMyAppointmentCommand(id, expectedUpdatedDate), ct)) return NotFound();
            if (Request.WantsJson()) return Json(new { success = true, id, status = "cancelled", message = "Randevunuz iptal edildi." });
            TempData["AppointmentNotice"] = "Randevunuz iptal edildi.";
        }
        catch (ValidationException e) { if (Request.WantsJson()) return BadRequest(new { success = false, message = e.Message }); TempData["AppointmentNotice"] = e.Message; }
        return RedirectToAction(nameof(Index));
    }
    private async Task Populate(MyAppointmentEditModel model, CancellationToken ct)
        => model.Services = await sender.Send(new GetReservationServicesQuery(), ct);
}
