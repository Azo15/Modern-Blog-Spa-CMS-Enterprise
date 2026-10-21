using BusinessLayer.Abstract;
using BusinessLayer.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MediatR;
using BusinessLayer.CQRS.Commands;
using BusinessLayer.CQRS.Queries;
using WebUI.Infrastructure;

namespace WebUI.Controllers
{
    [Authorize(Policy = AccessPolicies.AccountModerator)]
    public class AdminMessageController : BaseController
    {
        private readonly IContactFormService _contactFormService;
        private readonly ISender _sender;

        public AdminMessageController(IContactFormService contactFormService, ISender sender)
        {
            _contactFormService = contactFormService;
            _sender = sender;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1)
        {
            var query = _contactFormService.GetQueryable().AsNoTracking();
            var count = await query.CountAsync(HttpContext.RequestAborted);
            page = Math.Clamp(page, 1, Math.Max(1, (count + 49) / 50));
            var data = await query.OrderByDescending(x => x.CreatedDate).ThenByDescending(x => x.Id).Skip((page - 1) * 50).Take(50).ToListAsync(HttpContext.RequestAborted);
            ViewBag.Count = count; ViewBag.Page = page; ViewBag.Pages = Math.Max(1, (count + 49) / 50);
            return View(data);
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Details(int id, int page = 1)
        {
            var model = await _sender.Send(new GetContactMessageDetailsQuery(id, page), HttpContext.RequestAborted);
            return model is null ? NotFound() : View(model);
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> ReplyHistory(int id, int page = 1)
        {
            var model = await _sender.Send(new GetContactMessageDetailsQuery(id, page), HttpContext.RequestAborted);
            return model is null ? NotFound() : PartialView("_ReplyHistory", model);
        }

        [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(32768)]
        public async Task<IActionResult> Reply(int id, string? replyMessage, Guid clientRequestId)
        {
            try
            {
                if (!ModelState.IsValid) throw new FluentValidation.ValidationException("Yanıt bilgileri geçersiz.");
                var result = await _sender.Send(new ReplyContactMessageCommand(id, replyMessage ?? "", clientRequestId), HttpContext.RequestAborted);
                var message = result.Queued ? "Yanıt kaydedildi ve e-posta kuyruğuna alındı." : "Yanıt kaydedildi. E-posta kuyruğu geçici olarak erişilemiyor; otomatik yeniden denenecek.";
                if (Request.WantsJson()) return Json(new { success = true, message, replyId = result.Id, nextRequestId = Guid.NewGuid() });
                TempData["ReplyNotice"] = message;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (FluentValidation.ValidationException error)
            {
                if (Request.WantsJson()) return BadRequest(new { success = false, message = error.Message });
                ModelState.AddModelError("replyMessage", error.Message); ViewData["ReplyDraft"] = replyMessage;
                return await Details(id);
            }
        }
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var message = await _contactFormService.GetByIDAsync(id);
            if (message is null) return NotFound();
            message.IsRead = true;
            await _contactFormService.UpdateAsync(message);
            if (Request.WantsJson()) return Json(new { success = true, message = "Mesaj okundu olarak işaretlendi." });
            return RedirectToAction(nameof(Details), new { id });
        }

    }
}
