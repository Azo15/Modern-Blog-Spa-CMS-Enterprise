using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using BusinessLayer.Security;
using EntityLayer.Enum;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace WebUI.Controllers;

[Authorize(Policy = AccessPolicies.ContentManager)]
public sealed class AdminAppointmentController(ISender sender) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(AppointmentStatus? status = null, int page = 1, CancellationToken cancellationToken = default)
    {
        if (page < 1 || (status.HasValue && !Enum.IsDefined(status.Value))) return BadRequest();
        ViewData["StatusFilter"] = status;
        return View(await sender.Send(new GetAppointmentsQuery(status, page), cancellationToken));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(int id, CancellationToken cancellationToken) => ChangeStatus(id, AppointmentStatus.Confirmed, cancellationToken);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Cancel(int id, CancellationToken cancellationToken) => ChangeStatus(id, AppointmentStatus.Cancelled, cancellationToken);

    private async Task<IActionResult> ChangeStatus(int id, AppointmentStatus status, CancellationToken cancellationToken)
    {
        try
        {
            if (!await sender.Send(new ChangeAppointmentStatusCommand(id, status), cancellationToken)) return NotFound();
            TempData["AppointmentSuccess"] = status == AppointmentStatus.Confirmed ? "Randevu onaylandı." : "Randevu iptal edildi.";
        }
        catch (ValidationException error) { TempData["AppointmentError"] = string.Join(" ", error.Errors.Select(e => e.ErrorMessage)); }
        catch (DbUpdateConcurrencyException) { TempData["AppointmentError"] = "Bu randevu başka bir yönetici tarafından güncellendi. Güncel durumu kontrol edin."; }
        return RedirectToAction(nameof(Index));
    }
}
