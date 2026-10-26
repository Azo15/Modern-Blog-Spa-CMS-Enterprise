using System.Security.Claims;
using BusinessLayer.Abstract;
using BusinessLayer.Security;
using DataAccesLayer.Concrete;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebUI.Infrastructure;
namespace WebUI.Controllers;
[Authorize(Policy = AccessPolicies.AccountModerator),Route("admin/notifications"),ResponseCache(NoStore = true,Location = ResponseCacheLocation.None)]
public sealed class NotificationsController(INotificationService notifications,AppDbContext db) : Controller
{
    private async Task<int?> RecipientAsync(CancellationToken ct) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id) && await NotificationRecipients.CanReceiveAsync(db,id,ct) ? id : null;
    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1,CancellationToken ct = default)
    {
        var id = await RecipientAsync(ct); if (id is null) return Forbid();
        return View(await notifications.ListAsync(id.Value,page,25,ct));
    }
    [HttpGet("list")]
    public async Task<IActionResult> List(int page = 1,int size = 10,CancellationToken ct = default)
    {
        var id = await RecipientAsync(ct); if (id is null) return Forbid();
        return Json(await notifications.ListAsync(id.Value,page,size,ct));
    }
    [HttpGet("count")]
    public async Task<IActionResult> Count(CancellationToken ct)
    {
        var id = await RecipientAsync(ct); if (id is null) return Forbid();
        return Json(new { success = true,unreadCount = await notifications.CountAsync(id.Value,ct) });
    }
    [HttpPost("read"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Read(long id,CancellationToken ct)
    {
        var userId = await RecipientAsync(ct); if (userId is null) return Forbid();
        if (!ModelState.IsValid || id <= 0) return BadRequest(new { success = false,message = "Geçersiz bildirim." });
        if (!await notifications.ReadAsync(userId.Value,id,ct)) return NotFound(new { success = false,message = "Bildirim bulunamadı." });
        return Request.WantsJson() ? Json(new { success = true,unreadCount = await notifications.CountAsync(userId.Value,ct) }) : RedirectToAction(nameof(Index));
    }
    [HttpPost("read-all"),ValidateAntiForgeryToken]
    public async Task<IActionResult> ReadAll(long throughId,CancellationToken ct)
    {
        var userId = await RecipientAsync(ct); if (userId is null) return Forbid();
        if (!ModelState.IsValid || throughId < 0) return BadRequest(new { success = false,message = "Geçersiz bildirim aralığı." });
        await notifications.ReadAllAsync(userId.Value,throughId,ct);
        return Request.WantsJson() ? Json(new { success = true,unreadCount = await notifications.CountAsync(userId.Value,ct) }) : RedirectToAction(nameof(Index));
    }
}
