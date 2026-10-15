using System.Globalization;
using BusinessLayer.CQRS.Commands;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Models;

namespace WebUI.Controllers;

[AllowAnonymous]
[Route("randevu")]
public sealed class AppointmentController(ISender sender, ILogger<AppointmentController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index(int? serviceId)
        => Redirect("/?reservation=1" + (serviceId is > 0 ? "&serviceId=" + serviceId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty));

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(16384)]
    public async Task<IActionResult> Create(AppointmentRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Where(pair => pair.Value?.Errors.Count > 0)
                .ToDictionary(pair => pair.Key, pair => new[] { "Bu alanın değeri geçersiz. Lütfen kontrol edin." });
            return BadRequest(new { success = false, message = "Lütfen formdaki alanları kontrol edin.", errors });
        }
        try
        {
            var receipt = await sender.Send(new CreateAppointmentCommand(request.ServiceId, request.AppointmentDate,
                request.TimeSlot, request.GuestCount, request.FullName, request.Phone, request.Email, request.Notes, request.ClientRequestId), cancellationToken);
            return StatusCode(receipt.IsNew ? StatusCodes.Status201Created : StatusCodes.Status200OK, new
            {
                success = true, receipt.ReferenceCode, appointmentDate = receipt.AppointmentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                receipt.TimeSlot, status = receipt.Status.ToString(),
                message = "Randevu talebiniz başarıyla alındı, onay için sizinle iletişime geçilecektir."
            });
        }
        catch (ValidationException error)
        {
            return BadRequest(new { success = false, message = "Lütfen formdaki alanları kontrol edin.",
                errors = error.Errors.GroupBy(e => e.PropertyName).ToDictionary(group => group.Key, group => group.Select(e => e.ErrorMessage).Distinct().ToArray()) });
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            logger.LogError(error, "Appointment request could not be completed.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            { success = false, message = "Talebiniz tamamlanamadı. Lütfen aynı form üzerinden tekrar deneyin." });
        }
    }
}
